"""Fail closed before building a Python distribution with stale trust material."""

import hashlib
import os
import re
import ssl
import sys
from importlib import import_module, metadata
from pathlib import Path

_MINIMUM_PYTHON = {
    (3, 11): (3, 11, 18),
    (3, 12): (3, 12, 16),
    (3, 13): (3, 13, 18),
    (3, 14): (3, 14, 10),
    (3, 15): (3, 15, 2),
}
# https://github.com/advisories/GHSA-6q48-6c7w-9gw2
_AFFECTED_ROOT = "86a1ecba089c4a8d3bbe2734c612ba341d813e043cf9e8a862cd5c57a36bbe6b"
_CERTIFICATE = re.compile(
    r"-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----", re.DOTALL
)


def require_python(version: tuple[int, ...], releaselevel: str = "final") -> None:
    minimum = _MINIMUM_PYTHON.get(version[:2])
    if releaselevel != "final" or minimum is None or version[:3] < minimum:
        raise ValueError(f"unsupported or unpatched build Python: {version[:3]}")


def require_version(name: str, actual: str, minimum: tuple[int, ...]) -> None:
    # Pre-releases and unexpected version formats are not accepted for builds.
    if not re.fullmatch(r"\d+(?:\.\d+)*", actual):
        raise ValueError(f"unsupported {name} version: {actual}")
    version = tuple(int(part) for part in actual.split("."))
    if version < minimum:
        raise ValueError(f"{name} is too old: {actual}")


def check_bundle(label: str, path: str) -> None:
    pem = Path(path).read_text(encoding="ascii")
    certificates = _CERTIFICATE.findall(pem)
    if not certificates:
        raise ValueError(f"{label} CA bundle contains no certificates: {path}")
    for certificate in certificates:
        der = ssl.PEM_cert_to_DER_cert(certificate)
        if hashlib.sha256(der).hexdigest() == _AFFECTED_ROOT:
            raise ValueError(f"{label} CA bundle contains the affected root: {path}")
    print(f"{label} CA bundle: {path}")


def check_environment_bundles() -> None:
    # OpenSSL certificate directories are not PEM bundle files; do not silently
    # approve a trust source that this check cannot inspect.
    if os.environ.get("SSL_CERT_DIR"):
        raise ValueError("SSL_CERT_DIR is not supported by build trust preflight")
    for name in ("PIP_CERT", "REQUESTS_CA_BUNDLE", "CURL_CA_BUNDLE", "SSL_CERT_FILE"):
        path = os.environ.get(name)
        if path:
            check_bundle(name, path)


def verify_environment() -> None:
    require_python(sys.version_info[:3], sys.version_info.releaselevel)
    pip_version = metadata.version("pip")
    certifi_version = metadata.version("certifi")
    require_version("pip", pip_version, (26, 2, 2))
    require_version("certifi", certifi_version, (2026, 10, 19))
    pip_certifi = import_module("pip._vendor.certifi")
    pip = import_module("pip")
    certifi = import_module("certifi")
    if pip.__version__ != pip_version or certifi.__version__ != certifi_version:
        raise ValueError("imported pip/certifi does not match installed metadata")
    print(
        f"Python {sys.version.split()[0]}; pip {pip_version}; certifi {certifi_version}"
    )
    check_bundle("pip", pip_certifi.where())
    check_bundle("certifi", certifi.where())
    check_environment_bundles()


def main() -> int:
    try:
        verify_environment()
    except (ValueError, OSError, ImportError, metadata.PackageNotFoundError) as exc:
        print(f"Build trust preflight failed: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
