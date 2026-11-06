import subprocess
import unittest
from pathlib import Path
from unittest.mock import patch

from scripts import verify_windows_signature as verifier


class WindowsSignatureTests(unittest.TestCase):
    def setUp(self):
        self.root = Path(__file__).parent.absolute()
        self.target = self.root / "sample.exe"
        self.signtool = self.root / "signtool.exe"
        self.signer = "a" * 64
        self.metadata = {
            "status": "Valid",
            "signature_type": "Authenticode",
            "signer_sha256": self.signer,
            "timestamp_present": True,
        }
        self.path_checks = patch.object(
            verifier, "has_reparse_point", return_value=False
        )
        self.path_checks.start()
        self.addCleanup(self.path_checks.stop)
        self.file_checks = patch.object(Path, "is_file", return_value=True)
        self.file_checks.start()
        self.addCleanup(self.file_checks.stop)
        self.resolves = patch.object(
            Path, "resolve", lambda path, strict=False: path.absolute()
        )
        self.resolves.start()
        self.addCleanup(self.resolves.stop)
        self.hashes = patch.object(verifier, "checksum", return_value="c" * 64)
        self.hashes.start()
        self.addCleanup(self.hashes.stop)

    @patch.object(verifier.subprocess, "run")
    @patch.object(verifier, "read_authenticode")
    def test_accepts_exact_single_sha256_rfc3161_signature(self, read_metadata, run):
        read_metadata.return_value = self.metadata
        run.return_value = subprocess.CompletedProcess(
            args=[],
            returncode=0,
            stdout="Index  Algorithm  Timestamp\n0      sha256     RFC3161\nSuccessfully verified\n",
            stderr="",
        )

        digest = verifier.verify(self.target, self.signer, self.signtool)

        self.assertEqual(digest, "c" * 64)
        self.assertEqual(run.call_args.args[0][1:4], ["verify", "/pa", "/all"])
        self.assertIn("/tw", run.call_args.args[0])

    @patch.object(verifier, "read_authenticode")
    def test_rejects_invalid_signature_metadata(self, read_metadata):
        cases = [
            ("status", "NotSigned"),
            ("signature_type", "Catalog"),
            ("signer_sha256", "b" * 64),
            ("timestamp_present", False),
        ]
        for key, value in cases:
            with self.subTest(key=key):
                read_metadata.return_value = {**self.metadata, key: value}
                with self.assertRaises(ValueError):
                    verifier.verify(self.target, self.signer, self.signtool)

    @patch.object(verifier.subprocess, "run")
    @patch.object(verifier, "read_authenticode")
    def test_rejects_sign_tool_error_and_unexpected_summary(self, read_metadata, run):
        read_metadata.return_value = self.metadata
        for returncode, output in [
            (2, "Successfully verified"),
            (0, "Successfully verified without a timestamp"),
            (0, "0 sha1 RFC3161"),
            (0, "0 sha256 RFC3161\n1 sha256 RFC3161"),
            (0, "localized signature table"),
        ]:
            with self.subTest(returncode=returncode, output=output):
                run.return_value = subprocess.CompletedProcess(
                    args=[], returncode=returncode, stdout=output, stderr=""
                )
                with self.assertRaises(ValueError):
                    verifier.verify(self.target, self.signer, self.signtool)

        run.side_effect = subprocess.TimeoutExpired(cmd="signtool", timeout=30)
        with self.assertRaisesRegex(ValueError, "timed out"):
            verifier.verify(self.target, self.signer, self.signtool)

    @patch.object(verifier.subprocess, "run")
    @patch.object(verifier, "read_authenticode")
    def test_detects_file_change_during_sign_tool_check(self, read_metadata, run):
        read_metadata.return_value = self.metadata

        def mutate(*_args, **_kwargs):
            self.target.write_bytes(b"changed")
            return subprocess.CompletedProcess(
                args=[], returncode=0, stdout="0 sha256 RFC3161", stderr=""
            )

        run.side_effect = mutate
        self.hashes.stop()
        self.hashes = patch.object(
            verifier, "checksum", side_effect=["c" * 64, "d" * 64]
        )
        self.hashes.start()
        self.addCleanup(self.hashes.stop)
        with self.assertRaisesRegex(ValueError, "changed during verification"):
            verifier.verify(self.target, self.signer, self.signtool)

    def test_rejects_invalid_paths_and_fingerprint(self):
        with self.assertRaisesRegex(ValueError, "fingerprint"):
            verifier.verify(self.target, "not-a-fingerprint", self.signtool)
        with self.assertRaisesRegex(ValueError, "absolute path"):
            verifier.verify(self.target, self.signer, Path("signtool.exe"))
        with self.assertRaisesRegex(ValueError, "only .exe and .dll"):
            verifier.verify(self.root / "archive.zip", self.signer, self.signtool)

    def test_rejects_unknown_sign_tool_summary_rows(self):
        with self.assertRaisesRegex(ValueError, "unrecognized"):
            verifier.signature_rows("0 malformed summary row with extra data")


if __name__ == "__main__":
    unittest.main()

