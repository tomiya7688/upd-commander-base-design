"""Partially verify one Windows PE file's embedded Authenticode signature.

The caller must protect the file from modification while this command runs.
This check is not a complete release authorization gate.
"""

import argparse
import base64
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
from pathlib import Path

_FINGERPRINT = re.compile(r"[0-9a-fA-F]{64}")
_SIGNATURE_ROW = re.compile(r"\s*(\d+)\s+(\S+)\s+(\S+)\s*")
_POWERSHELL = r"""
$ErrorActionPreference = 'Stop'
$signature = Get-AuthenticodeSignature -LiteralPath $env:UPD_SIGNATURE_TARGET -ErrorAction Stop
$fingerprint = $null
if ($null -ne $signature.SignerCertificate) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = [BitConverter]::ToString($sha256.ComputeHash($signature.SignerCertificate.RawData)).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha256.Dispose()
    }
}
@{
    status = $signature.Status.ToString()
    signature_type = $signature.SignatureType.ToString()
    signer_sha256 = $fingerprint
    timestamp_present = ($null -ne $signature.TimeStamperCertificate)
} | ConvertTo-Json -Compress
"""


def checksum(path: Path) -> str:
    """Hash a file in bounded chunks."""
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def has_reparse_point(path: Path) -> bool:
    """Reject symlinks and Windows reparse points before following them."""
    attributes = getattr(path.lstat(), "st_file_attributes", 0)
    reparse_flag = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0)
    return path.is_symlink() or bool(attributes & reparse_flag)


def read_authenticode(path: Path) -> dict[str, object]:
    """Read embedded-signature metadata with the inbox Windows PowerShell."""
    if sys.platform != "win32":
        raise ValueError("Authenticode verification requires Windows")
    system_root = os.environ.get("SystemRoot")
    if not system_root:
        raise ValueError("SystemRoot is required to locate Windows PowerShell")
    powershell = Path(system_root) / "System32/WindowsPowerShell/v1.0/powershell.exe"
    encoded = base64.b64encode(_POWERSHELL.encode("utf-16le")).decode("ascii")
    environment = os.environ.copy()
    environment["UPD_SIGNATURE_TARGET"] = str(path)
    try:
        result = subprocess.run(
            [
                str(powershell),
                "-NoProfile",
                "-NonInteractive",
                "-EncodedCommand",
                encoded,
            ],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            env=environment,
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise ValueError(
            "Windows PowerShell timed out reading Authenticode metadata"
        ) from exc
    if result.returncode != 0:
        raise ValueError("Windows PowerShell could not read Authenticode metadata")
    try:
        metadata = json.loads(result.stdout)
    except json.JSONDecodeError as exc:
        raise ValueError(
            "Windows PowerShell returned invalid signature metadata"
        ) from exc
    if not isinstance(metadata, dict):
        raise ValueError("Windows PowerShell returned invalid signature metadata")
    return metadata


def signature_rows(output: str) -> list[tuple[int, str, str]]:
    """Parse SignTool's signature summary; unknown formats fail closed."""
    rows = []
    for line in output.splitlines():
        match = _SIGNATURE_ROW.fullmatch(line)
        if match:
            index, algorithm, timestamp = match.groups()
            rows.append((int(index), algorithm.casefold(), timestamp.casefold()))
        elif re.match(r"\s*\d+\s+", line):
            raise ValueError("SignTool returned an unrecognized signature summary")
    return rows


def verify(path: Path, expected_signer: str, signtool: Path) -> str:
    """Require one valid embedded SHA-256/RFC3161 signature from the pinned signer."""
    if not _FINGERPRINT.fullmatch(expected_signer):
        raise ValueError(
            "expected signer must be a full 64-character certificate SHA-256 fingerprint"
        )
    if path.suffix.casefold() not in {".exe", ".dll"}:
        raise ValueError("only .exe and .dll PE files are supported")
    if has_reparse_point(path) or not path.is_file():
        raise ValueError("target must be an existing regular non-reparse-point file")
    if not signtool.is_absolute() or signtool.name.casefold() != "signtool.exe":
        raise ValueError("provide an absolute path to a trusted signtool.exe")
    if has_reparse_point(signtool) or not signtool.is_file():
        raise ValueError("SignTool must be an existing regular non-reparse-point file")

    path = path.resolve(strict=True)
    signtool = signtool.resolve(strict=True)
    initial_hash = checksum(path)
    metadata = read_authenticode(path)
    if metadata.get("status") != "Valid":
        raise ValueError("Windows does not report a valid Authenticode signature")
    if metadata.get("signature_type") != "Authenticode":
        raise ValueError(
            "a catalog signature is not an embedded Authenticode signature"
        )
    signer_fingerprint = metadata.get("signer_sha256")
    if (
        not isinstance(signer_fingerprint, str)
        or signer_fingerprint.casefold() != expected_signer.casefold()
    ):
        raise ValueError(
            "signer certificate fingerprint does not match the pinned value"
        )
    if metadata.get("timestamp_present") is not True:
        raise ValueError("Windows did not report a timestamp certificate")

    try:
        result = subprocess.run(
            [str(signtool), "verify", "/pa", "/all", "/tw", str(path)],
            check=False,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=30,
        )
    except subprocess.TimeoutExpired as exc:
        raise ValueError("SignTool verification timed out") from exc
    if result.returncode != 0:
        raise ValueError(
            f"SignTool verification failed with exit code {result.returncode}"
        )
    rows = signature_rows(result.stdout + "\n" + result.stderr)
    if rows != [(0, "sha256", "rfc3161")]:
        raise ValueError(
            "expected exactly one SHA-256 signature with an RFC 3161 timestamp"
        )
    if checksum(path) != initial_hash:
        raise ValueError("target file changed during verification")
    return initial_hash


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("file", type=Path)
    parser.add_argument("--signer-sha256", required=True)
    parser.add_argument("--signtool", type=Path, required=True)
    parser.add_argument(
        "--trust-immutable-file",
        action="store_true",
        required=True,
        help="acknowledge that the caller prevents concurrent modification; this tool does not enforce it",
    )
    args = parser.parse_args()
    try:
        digest = verify(args.file, args.signer_sha256, args.signtool)
    except (OSError, ValueError, subprocess.SubprocessError) as exc:
        print(f"Windows signature verification failed: {exc}", file=sys.stderr)
        return 1
    print(
        f"Embedded signature checks passed for SHA-256 {digest}. "
        "This partial check does not prove offline revocation behavior, verify every "
        "release asset, or authorize a release."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
