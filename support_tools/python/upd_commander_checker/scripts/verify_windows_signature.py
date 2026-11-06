"""Partially verify one Windows PE file's embedded Authenticode signature.

The caller must protect the file from modification while this command runs.
This check is not a complete release authorization gate.
"""

import argparse
import subprocess
import sys
from pathlib import Path

try:
    from .windows_signature_support import (
        _FINGERPRINT,
        checksum,
        read_authenticode,
        reject_reparse_components,
        require_local_path,
        signature_rows,
    )
except ImportError:
    from windows_signature_support import (
        _FINGERPRINT,
        checksum,
        read_authenticode,
        reject_reparse_components,
        require_local_path,
        signature_rows,
    )


def verify(path: Path, expected_signer: str, signtool: Path) -> str:
    """Require one valid embedded SHA-256/RFC3161 signature from the pinned signer."""
    if not _FINGERPRINT.fullmatch(expected_signer):
        raise ValueError(
            "expected signer must be a full 64-character certificate SHA-256 fingerprint"
        )
    if path.suffix.casefold() not in {".exe", ".dll"}:
        raise ValueError("only .exe and .dll PE files are supported")
    if not signtool.is_absolute() or signtool.name.casefold() != "signtool.exe":
        raise ValueError("provide an absolute path to a trusted signtool.exe")
    path = require_local_path(path)
    signtool = require_local_path(signtool)
    reject_reparse_components(path)
    reject_reparse_components(signtool)
    if not path.is_file():
        raise ValueError("target must be an existing regular non-reparse-point file")
    if signtool.name.casefold() != "signtool.exe" or not signtool.is_file():
        raise ValueError("SignTool must be an existing regular non-reparse-point file")

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
            [str(signtool), "verify", "/v", "/pa", "/all", "/tw", str(path)],
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
        help="acknowledge that the caller prevents concurrent file and path modification; this tool does not enforce it",
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
