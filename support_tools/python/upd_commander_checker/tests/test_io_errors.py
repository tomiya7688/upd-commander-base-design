import os
from pathlib import Path
import tempfile
import unittest

from upd_commander_checker.scanner import scan_path


class IoErrorTest(unittest.TestCase):
    def test_missing_ignore_file_is_normal(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "plain.py").write_text("value = 1\n", encoding="utf-8")

            findings = scan_path(root)

        self.assertFalse(any(item.code == "UPD001" for item in findings))

    @unittest.skipIf(os.name == "nt", "Unix permissions are required")
    def test_unreadable_ignore_file_reports_upd001_and_stops_scan(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            ignore_path = root / ".updcommanderignore"
            ignore_path.write_text("generated/**\n", encoding="utf-8")
            (root / "broken.py").write_text("def broken(\n", encoding="utf-8")
            os.chmod(ignore_path, 0)
            try:
                try:
                    ignore_path.read_text(encoding="utf-8")
                except OSError:
                    pass
                else:
                    self.skipTest("current user bypasses file permissions")
                findings = scan_path(root)
            finally:
                os.chmod(ignore_path, 0o600)

        self.assertTrue(any(item.code == "UPD001" for item in findings))
        self.assertFalse(any(item.code == "UPD002" for item in findings))

    @unittest.skipIf(os.name == "nt", "Unix permissions are required")
    def test_unreadable_directory_reports_upd001_and_continues(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            blocked = root / "blocked"
            blocked.mkdir()
            (blocked / "hidden.py").write_text("value = 1\n", encoding="utf-8")
            process = root / "process"
            process.mkdir()
            (process / "loop_commander.py").write_text(
                "class LoopCommander:\n"
                "    def run(self):\n"
                "        for _ in range(3):\n"
                "            pass\n",
                encoding="utf-8",
            )
            os.chmod(blocked, 0)
            try:
                try:
                    list(blocked.iterdir())
                except OSError:
                    pass
                else:
                    self.skipTest("current user bypasses directory permissions")
                findings = scan_path(root)
            finally:
                os.chmod(blocked, 0o700)

        self.assertTrue(any(item.code == "UPD001" for item in findings))
        self.assertTrue(any(item.code == "UPD201" for item in findings))


if __name__ == "__main__":
    unittest.main()
