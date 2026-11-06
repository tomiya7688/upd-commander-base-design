import hashlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from scripts import verify_release_attestations as verifier


class ReleaseAttestationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.asset = self.directory / "checker.zip"
        self.asset.write_bytes(b"artifact")
        self.digest = hashlib.sha256(b"artifact").hexdigest()
        self.manifest = self.directory / "SHA256SUMS"
        self.manifest.write_text(f"{self.digest}  checker.zip\n", encoding="ascii")
        self.workflow = verifier.REPOSITORY + "/.github/workflows/release.yml"
        self.source = "a" * 40
        self.signer = "b" * 40

    def run_verify(self):
        verifier.verify(self.directory, self.workflow, self.source, self.signer)

    @patch.object(verifier.subprocess, "run")
    def test_manifest_and_asset_both_require_pinned_attestations(self, run):
        self.run_verify()
        self.assertEqual(run.call_count, 2)
        for call in run.call_args_list:
            args = call.args[0]
            self.assertIn("--signer-workflow", args)
            self.assertIn(self.workflow, args)
            self.assertIn("--source-digest", args)
            self.assertIn(self.source, args)
            self.assertIn("--signer-digest", args)
            self.assertIn(self.signer, args)
            self.assertIn("--deny-self-hosted-runners", args)
            self.assertTrue(call.kwargs["check"])

    @patch.object(verifier, "verify_attestation")
    def test_rejects_wrong_checksum_and_manifest_traversal(self, attest):
        self.asset.write_bytes(b"modified")
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            self.run_verify()
        self.manifest.write_text(f"{self.digest}  ../checker.zip\n", encoding="ascii")
        with self.assertRaisesRegex(ValueError, "invalid SHA256SUMS"):
            self.run_verify()
        self.assertEqual(attest.call_count, 2)

    @patch.object(verifier, "verify_attestation")
    def test_rejects_unlisted_files_and_directories(self, _attest):
        extra = self.directory / "unlisted.bin"
        extra.write_bytes(b"unlisted")
        with self.assertRaisesRegex(ValueError, "unlisted=\\['unlisted.bin'\\]"):
            self.run_verify()

        extra.unlink()
        nested = self.directory / "nested"
        nested.mkdir()
        with self.assertRaisesRegex(ValueError, "non-regular entry"):
            self.run_verify()

    @patch.object(verifier, "verify_attestation")
    def test_rejects_inventory_change_during_verification(self, attest):
        extra = self.directory / "added-during-verification.bin"

        def add_file_after_manifest(path, *_args):
            if path.name != "SHA256SUMS":
                extra.write_bytes(b"late file")

        attest.side_effect = add_file_after_manifest
        with self.assertRaisesRegex(
            ValueError, "unlisted=\\['added-during-verification.bin'\\]"
        ):
            self.run_verify()

    @patch.object(verifier, "verify_attestation")
    def test_rejects_duplicates_and_empty_manifest(self, _attest):
        self.manifest.write_text(
            f"{self.digest}  checker.zip\n{self.digest}  CHECKER.ZIP\n",
            encoding="ascii",
        )
        with self.assertRaisesRegex(ValueError, "duplicate"):
            self.run_verify()
        self.manifest.write_text("", encoding="ascii")
        with self.assertRaisesRegex(ValueError, "at least one"):
            self.run_verify()

    @patch.object(verifier, "verify_attestation")
    def test_rejects_changed_manifest(self, attest):
        attest.side_effect = lambda *_: self.manifest.write_text(
            "changed", encoding="ascii"
        )
        with self.assertRaisesRegex(ValueError, "manifest changed"):
            self.run_verify()

    @patch.object(verifier, "verify_attestation")
    def test_rejects_unpinned_workflow_and_commit(self, attest):
        with self.assertRaises(ValueError):
            verifier.verify(
                self.directory,
                "other/repo/.github/workflows/release.yml",
                self.source,
                self.signer,
            )
        with self.assertRaises(ValueError):
            verifier.verify(self.directory, self.workflow, "main", self.signer)
        attest.assert_not_called()

    @patch.object(verifier.subprocess, "run")
    def test_attestation_failure_stops_verification(self, run):
        run.side_effect = subprocess.CalledProcessError(1, "gh")
        with self.assertRaises(subprocess.CalledProcessError):
            self.run_verify()
        self.assertEqual(run.call_count, 1)

    @patch.object(verifier, "verify_attestation")
    def test_rejects_windows_device_and_missing_asset(self, _attest):
        self.manifest.write_text(f"{self.digest}  NUL.zip\n", encoding="ascii")
        with self.assertRaisesRegex(ValueError, "invalid asset name"):
            self.run_verify()
        self.manifest.write_text(f"{self.digest}  missing.zip\n", encoding="ascii")
        with self.assertRaisesRegex(ValueError, "missing"):
            self.run_verify()

    @patch.object(
        sys,
        "argv",
        [
            "verify_release_attestations.py",
            ".",
            "--workflow",
            "x",
            "--source",
            "x",
            "--signer",
            "x",
        ],
    )
    def test_cli_requires_immutable_directory_acknowledgement(self):
        with self.assertRaises(SystemExit) as error:
            verifier.main()
        self.assertEqual(error.exception.code, 2)


if __name__ == "__main__":
    unittest.main()
