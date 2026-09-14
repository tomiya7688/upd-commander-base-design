import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class DataTypeLocationTests(unittest.TestCase):
    def test_data_type_next_to_behavioral_type_is_attention(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "service.py").write_text(
                "class Payload:\n    value: int\n\nclass Service:\n    def run(self):\n        return 1\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            payload = [item for item in findings if "Payload" in item.message]
            self.assertEqual(len(payload), 1)
            self.assertEqual(payload[0].code, "UPD403")
            self.assertEqual(payload[0].severity, "attention")

    def test_external_reference_promotes_to_warning(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "service.py").write_text(
                "class Payload:\n    value: int\n\nclass Service:\n    def run(self):\n        return 1\n",
                encoding="utf-8",
            )
            (root / "consumer.py").write_text(
                "from service import Payload\n\ndef use(value: Payload):\n    return value\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            payload = [item for item in findings if "Payload" in item.message]
            self.assertEqual(len(payload), 1)
            self.assertEqual(payload[0].code, "UPD404")
            self.assertEqual(payload[0].severity, "warning")

    def test_multiple_data_only_types_alone_are_allowed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "models.py").write_text(
                "class First:\n    value: int\n\nclass Second:\n    value: int\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code in {"UPD403", "UPD404"} for item in findings))


if __name__ == "__main__":
    unittest.main()
