import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class CheckerTest(unittest.TestCase):
    def test_syntax_error_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "sample.py"
            path.write_text("def broken(:\n", encoding="utf-8")

            findings = scan_path(path)

            self.assertEqual("UPD002", findings[0].code)

    def test_ui_to_data_import_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "ui" / "screen_processing.py"
            path.parent.mkdir()
            path.write_text("from data.storage import load\n", encoding="utf-8")

            findings = scan_path(root)

            self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_clean_module_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "battle_processing.py"
            path.parent.mkdir()
            path.write_text("def calculate(value: int) -> int:\n    return value + 1\n", encoding="utf-8")

            findings = scan_path(root)

            self.assertEqual([], findings)


if __name__ == "__main__":
    unittest.main()
