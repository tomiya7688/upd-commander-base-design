import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class RegressionMatrixTests(unittest.TestCase):
    def test_commander_loop_and_direct_work(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "rule_commander.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "def run():\n    for _ in range(2):\n        pass\n    open('sample.txt')\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD201" for item in findings))
            self.assertTrue(any(item.code == "UPD203" for item in findings))

    def test_container_rules(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "large_commander.py"
            path.parent.mkdir(parents=True)
            parameters = ",\n        ".join(f"p{index}" for index in range(12))
            path.write_text(
                "class LargeCommander:\n"
                "    def run(\n"
                f"        self,\n        {parameters}\n"
                "    ):\n"
                "        return p0, p1\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD301" for item in findings))
            self.assertTrue(any(item.code == "UPD302" for item in findings))
            self.assertTrue(any(item.code == "UPD303" for item in findings))

    def test_cli_path_ignore(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "ignored_commander.py"
            path.parent.mkdir(parents=True)
            path.write_text("value = left + right\n", encoding="utf-8")
            findings = scan_path(root, ("process/**",))
            self.assertFalse(any(item.code == "UPD202" for item in findings))


if __name__ == "__main__":
    unittest.main()
