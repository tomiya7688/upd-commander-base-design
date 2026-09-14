import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class DataTypeLocationTests(unittest.TestCase):
    def test_local_only_data_types_are_attention(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD403" and item.severity == "attention" for item in findings))
            self.assertFalse(any(item.code == "UPD404" for item in findings))

    def test_external_reference_promotes_to_warning(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            (root / "consumer.py").write_text(
                "from models import First\n\ndef use(value: First):\n    return value\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            first = [item for item in findings if item.code in {"UPD403", "UPD404"} and "First" in item.message]
            self.assertEqual(len(first), 1)
            self.assertEqual(first[0].code, "UPD404")
            self.assertEqual(first[0].severity, "warning")


if __name__ == "__main__":
    unittest.main()
