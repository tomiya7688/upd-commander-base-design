"""Windows path and signature metadata helpers for the partial verifier."""

import base64
import ctypes
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


def windows_system_directory() -> Path:
    """Get the system directory from Windows instead of trusting environment paths."""
    if sys.platform != "win32":
        raise ValueError("Authenticode verification requires Windows")
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    get_system_directory = kernel32.GetSystemDirectoryW
    get_system_directory.argtypes = [ctypes.POINTER(ctypes.c_wchar), ctypes.c_uint]
    get_system_directory.restype = ctypes.c_uint
    buffer = ctypes.create_unicode_buffer(32768)
    length = get_system_directory(buffer, len(buffer))
    if length == 0 or length >= len(buffer):
        raise ValueError("Windows did not return its system directory")
    return Path(buffer.value)


def require_local_path(path: Path) -> Path:
    """Return an absolute local path and reject UNC or remote-drive inputs."""
    if str(path).startswith(("\\\\", "//")):
        raise ValueError("network paths are not supported")
    absolute = Path(os.path.abspath(path))
    if sys.platform == "win32":
        if not absolute.anchor or absolute.anchor.startswith("\\\\"):
            raise ValueError("network paths are not supported")
        kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        get_drive_type = kernel32.GetDriveTypeW
        get_drive_type.argtypes = [ctypes.c_wchar_p]
        get_drive_type.restype = ctypes.c_uint
        drive_type = get_drive_type(absolute.anchor)
        if drive_type not in {2, 3, 5, 6}:
            raise ValueError("unknown or remote drives are not supported")
    return absolute


def reject_reparse_components(path: Path) -> None:
    """Reject reparse points in the file path before opening or resolving it."""
    for component in (path, *path.parents):
        if has_reparse_point(component):
            raise ValueError("target and parent paths must not contain reparse points")


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
    system_directory = windows_system_directory()
    powershell = system_directory / "WindowsPowerShell/v1.0/powershell.exe"
    encoded = base64.b64encode(_POWERSHELL.encode("utf-16le")).decode("ascii")
    environment = os.environ.copy()
    environment["UPD_SIGNATURE_TARGET"] = str(path)
    environment["SystemRoot"] = str(system_directory.parent)
    environment["WINDIR"] = str(system_directory.parent)
    environment["PATH"] = str(system_directory)
    environment["PSModulePath"] = str(powershell.parent / "Modules")
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
            cwd=str(powershell.parent),
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
        if re.search(r"\b(?:warning|error)\b", line, re.IGNORECASE):
            raise ValueError("SignTool reported a warning or error")
        match = _SIGNATURE_ROW.fullmatch(line)
        if match:
            index, algorithm, timestamp = match.groups()
            rows.append((int(index), algorithm.casefold(), timestamp.casefold()))
        elif re.match(r"\s*\d+\s+", line):
            raise ValueError("SignTool returned an unrecognized signature summary")
    return rows
