"""Verify listed files only; callers must prevent concurrent modification."""

import argparse
import hashlib
import re
import subprocess
import sys
from pathlib import Path

REPOSITORY = "tomiya7688/upd-commander-base-design"
_ENTRY = re.compile(r"([0-9a-f]{64})  ([A-Za-z0-9][A-Za-z0-9._-]*)")
_SHA = re.compile(r"[0-9a-f]{40}")
_WINDOWS_DEVICE = re.compile(r"(?:con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?", re.I)


def checksum(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def verify_attestation(path: Path, workflow: str, source: str, signer: str) -> None:
    subprocess.run(
        [
            "gh",
            "attestation",
            "verify",
            str(path),
            "--repo",
            REPOSITORY,
            "--signer-workflow",
            workflow,
            "--source-digest",
            source,
            "--signer-digest",
            signer,
            "--deny-self-hosted-runners",
        ],
        check=True,
    )


def verify(directory: Path, workflow: str, source: str, signer: str) -> None:
    prefix = f"{REPOSITORY}/.github/workflows/"
    if not workflow.startswith(prefix) or not re.fullmatch(
        r"[A-Za-z0-9_-]+\.ya?ml", workflow.removeprefix(prefix)
    ):
        raise ValueError("expected a workflow path in this repository")
    if not _SHA.fullmatch(source) or not _SHA.fullmatch(signer):
        raise ValueError("source and signer must be full lowercase commit SHAs")
    if directory.is_symlink() or not directory.is_dir():
        raise ValueError("artifact directory must be an existing non-symlink directory")
    directory = directory.resolve(strict=True)
    manifest = directory / "SHA256SUMS"
    if manifest.is_symlink() or not manifest.is_file():
        raise ValueError("SHA256SUMS must be a regular non-symlink file")
    manifest_hash = checksum(manifest)
    verify_attestation(manifest, workflow, source, signer)
    raw = manifest.read_text(encoding="ascii")
    if checksum(manifest) != manifest_hash:
        raise ValueError("manifest changed during verification")
    entries = {}
    for line in raw.splitlines():
        match = _ENTRY.fullmatch(line)
        if not match:
            raise ValueError("invalid SHA256SUMS entry")
        digest, name = match.groups()
        key = name.casefold()
        if (
            key in entries
            or key == "sha256sums"
            or name.endswith(".")
            or _WINDOWS_DEVICE.fullmatch(name)
        ):
            raise ValueError("duplicate or invalid asset name")
        entries[key] = (name, digest)
    if not entries:
        raise ValueError("SHA256SUMS must list at least one asset")
    for name, expected in entries.values():
        asset = directory / name
        if asset.is_symlink() or not asset.is_file():
            raise ValueError(f"asset is missing or not a regular file: {name}")
        if checksum(asset) != expected:
            raise ValueError(f"checksum mismatch: {name}")
        verify_attestation(asset, workflow, source, signer)
        if checksum(asset) != expected:
            raise ValueError(f"asset changed during verification: {name}")
    if checksum(manifest) != manifest_hash:
        raise ValueError("manifest changed during verification")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--workflow", required=True)
    parser.add_argument("--source", required=True)
    parser.add_argument("--signer", required=True)
    parser.add_argument(
        "--trust-immutable-directory",
        action="store_true",
        required=True,
        help="acknowledge that the caller prevents modification during verification; this tool does not enforce it",
    )
    args = parser.parse_args()
    try:
        verify(args.directory, args.workflow, args.source, args.signer)
    except (ValueError, OSError, UnicodeError, subprocess.CalledProcessError) as exc:
        print(f"attestation verification failed: {exc}", file=sys.stderr)
        return 1
    print(
        "Listed files passed attestation and checksum checks, assuming the directory "
        "was not modified. Unlisted files and concurrent modification were not checked; "
        "this does not authorize a release."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
