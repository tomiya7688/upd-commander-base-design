import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class ExternalDependencyTest(unittest.TestCase):
    def test_external_data_name_does_not_trigger_layer_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            path.write_text("import thirdparty.data.client\n", encoding="utf-8")

            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD101" for item in findings))

    def test_external_processing_name_does_not_trigger_role_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "event_messenger.py"
            path.parent.mkdir(parents=True)
            path.write_text("import vendor.processing.engine\n", encoding="utf-8")

            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD101" for item in findings))

    def test_missing_other_application_module_is_not_internal(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "main" / "process" / "main_commander.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "import applications.external.data.client\n",
                encoding="utf-8",
            )

            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD102" for item in findings))


if __name__ == "__main__":
    unittest.main()
