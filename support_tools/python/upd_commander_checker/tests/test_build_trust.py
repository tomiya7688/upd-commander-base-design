import hashlib
import importlib
import os
import sys
import unittest
from collections import namedtuple
from types import SimpleNamespace
from unittest.mock import Mock, call, patch

from scripts import check_build_trust as trust


class BuildTrustTests(unittest.TestCase):
    def test_environment_ca_bundle_files_are_checked(self):
        values = {
            "PIP_CERT": "pip-custom.pem",
            "REQUESTS_CA_BUNDLE": "requests.pem",
            "CURL_CA_BUNDLE": "curl.pem",
            "SSL_CERT_FILE": "ssl.pem",
        }
        with (
            patch.dict(os.environ, values, clear=True),
            patch.object(trust, "check_bundle") as check,
        ):
            trust.check_environment_bundles()
        self.assertEqual(
            check.call_args_list, [call(name, path) for name, path in values.items()]
        )

    def test_environment_directory_is_rejected(self):
        with (
            patch.dict(os.environ, {"SSL_CERT_DIR": "ca-directory"}, clear=True),
            self.assertRaisesRegex(ValueError, "SSL_CERT_DIR"),
        ):
            trust.check_environment_bundles()

    def test_affected_environment_bundle_is_rejected(self):
        with (
            patch.dict(os.environ, {"PIP_CERT": "affected.pem"}, clear=True),
            patch.object(
                trust, "check_bundle", side_effect=ValueError("affected root")
            ),
            self.assertRaisesRegex(ValueError, "affected root"),
        ):
            trust.check_environment_bundles()

    def test_build_script_can_be_imported_as_a_module(self):
        build = importlib.import_module("scripts.build_exe")
        self.assertIs(build.verify_environment, trust.verify_environment)

    def test_build_stops_before_pyinstaller_if_preflight_fails(self):
        build = importlib.import_module("scripts.build_exe")
        installer = SimpleNamespace(__main__=SimpleNamespace(run=Mock()))
        with (
            patch.object(
                build, "verify_environment", side_effect=ValueError("blocked")
            ),
            patch.dict(
                sys.modules,
                {"PyInstaller": installer, "PyInstaller.__main__": installer.__main__},
            ),
            self.assertRaisesRegex(ValueError, "blocked"),
        ):
            build.main()
        installer.__main__.run.assert_not_called()

    def test_python_requires_a_known_patched_release(self):
        with self.assertRaises(ValueError):
            trust.require_python((3, 14, 10), "candidate")
        for version in ((3, 11, 17), (3, 14, 7), (3, 15, 1), (3, 10, 20), (3, 16, 0)):
            with self.subTest(version=version), self.assertRaises(ValueError):
                trust.require_python(version)
        for version in ((3, 11, 18), (3, 12, 16), (3, 13, 18), (3, 14, 10), (3, 15, 2)):
            with self.subTest(version=version):
                trust.require_python(version)

    def test_versions_must_meet_minimum_and_be_stable(self):
        trust.require_version("pip", "26.2.2", (26, 2, 2))
        trust.require_version("certifi", "2026.10.23", (2026, 10, 19))
        for version in ("26.2.1", "26.2.2rc1", "unknown"):
            with self.subTest(version=version), self.assertRaises(ValueError):
                trust.require_version("pip", version, (26, 2, 2))

    def test_bundle_rejects_affected_certificate_and_empty_bundle(self):
        pem = "-----BEGIN CERTIFICATE-----\nfixture\n-----END CERTIFICATE-----"
        fingerprint = hashlib.sha256(b"fixture").hexdigest()
        with (
            patch.object(trust.Path, "read_text", return_value=pem),
            patch.object(trust.ssl, "PEM_cert_to_DER_cert", return_value=b"fixture"),
            patch.object(trust, "_AFFECTED_ROOT", fingerprint),
            self.assertRaisesRegex(ValueError, "affected root"),
        ):
            trust.check_bundle("test", "unused")
        with (
            patch.object(trust.Path, "read_text", return_value=""),
            self.assertRaisesRegex(ValueError, "no certificates"),
        ):
            trust.check_bundle("test", "unused")

    def test_preflight_rejects_the_local_unpatched_python_before_imports(self):
        version_info = namedtuple(
            "VersionInfo", "major minor micro releaselevel serial"
        )
        with (
            patch.object(trust.sys, "version_info", version_info(3, 14, 7, "final", 0)),
            patch.object(trust, "import_module") as importer,
            self.assertRaisesRegex(ValueError, "unpatched build Python"),
        ):
            trust.verify_environment()
        importer.assert_not_called()

    def test_preflight_checks_both_installed_ca_bundles(self):
        version_info = namedtuple(
            "VersionInfo", "major minor micro releaselevel serial"
        )
        modules = {
            "pip._vendor.certifi": SimpleNamespace(where=lambda: "pip.pem"),
            "pip": SimpleNamespace(__version__="26.2.2"),
            "certifi": SimpleNamespace(
                __version__="2026.10.23", where=lambda: "certifi.pem"
            ),
        }
        with (
            patch.object(
                trust.sys, "version_info", version_info(3, 14, 10, "final", 0)
            ),
            patch.object(
                trust.metadata, "version", side_effect=("26.2.2", "2026.10.23")
            ),
            patch.object(trust, "import_module", side_effect=modules.__getitem__),
            patch.object(trust, "check_bundle") as check_bundle,
            patch.dict(os.environ, {}, clear=True),
        ):
            trust.verify_environment()
        self.assertEqual(
            check_bundle.call_args_list,
            [call("pip", "pip.pem"), call("certifi", "certifi.pem")],
        )


if __name__ == "__main__":
    unittest.main()
