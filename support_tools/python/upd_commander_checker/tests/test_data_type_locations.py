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

    def test_same_name_local_variable_does_not_promote(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            (root / "consumer.py").write_text(
                "def use():\n    First = 1\n    return First\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            first = [item for item in findings if "First" in item.message]
            self.assertEqual(len(first), 1)
            self.assertEqual(first[0].code, "UPD403")

    def test_same_name_type_from_other_module_does_not_promote(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            (root / "other.py").write_text("class First:\n    pass\n", encoding="utf-8")
            (root / "consumer.py").write_text(
                "from other import First\n\ndef use(value: First):\n    return value\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            first = [item for item in findings if item.relative == Path("models.py") and "First" in item.message]
            self.assertEqual(len(first), 1)
            self.assertEqual(first[0].code, "UPD403")

    def test_module_alias_reference_promotes_exact_type(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            (root / "consumer.py").write_text(
                "import models as model_types\n\ndef use(value: model_types.First):\n    return value\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            first = [item for item in findings if "First" in item.message]
            second = [item for item in findings if "Second" in item.message]
            self.assertEqual(first[0].code, "UPD404")
            self.assertEqual(second[0].code, "UPD403")


if __name__ == "__main__":
    unittest.main()
