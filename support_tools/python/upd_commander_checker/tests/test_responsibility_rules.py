import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class ResponsibilityRuleTest(unittest.TestCase):
    def test_large_class_reports_upd401(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "large.py"
            methods = "\n".join(
                f"    def method_{index}(self):\n        return {index}"
                for index in range(13)
            )
            path.write_text(f"class Large:\n{methods}\n", encoding="utf-8")
            findings = scan_path(path)
            self.assertTrue(any(item.code == "UPD401" for item in findings))

    def test_multiple_behavior_classes_report_upd402(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "mixed.py"
            path.write_text(
                "class Reader:\n    def read(self):\n        return 1\n\n"
                "class Writer:\n    def write(self):\n        return 2\n",
                encoding="utf-8",
            )
            findings = scan_path(path)
            self.assertTrue(any(item.code == "UPD402" for item in findings))


if __name__ == "__main__":
    unittest.main()
