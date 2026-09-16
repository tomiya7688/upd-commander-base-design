import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class SharedApplicationBoundaryTest(unittest.TestCase):
    def test_shared_processing_is_not_boundary_api(self) -> None:
        findings = self._scan_dependency("shared/process/settings_processing.py")
        self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_shared_data_is_not_boundary_api(self) -> None:
        findings = self._scan_dependency("shared/data/storage.py")
        self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_explicit_shared_contract_is_allowed(self) -> None:
        findings = self._scan_dependency("shared/contracts/process/settings_processing.py")
        self.assertFalse(any(item.code == "UPD102" for item in findings))

    def test_explicit_shared_message_is_allowed(self) -> None:
        findings = self._scan_dependency("shared/messages/process/settings_processing.py")
        self.assertFalse(any(item.code == "UPD102" for item in findings))

    def _scan_dependency(self, target_suffix: str):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            target = root / "applications" / "settings" / target_suffix
            target.parent.mkdir(parents=True)
            target.write_text("def run():\n    pass\n", encoding="utf-8")

            source = root / "applications" / "main" / "process" / "main_commander.py"
            source.parent.mkdir(parents=True)
            module = target.relative_to(root).with_suffix("").as_posix().replace("/", ".")
            source.write_text(f"import {module}\n", encoding="utf-8")
            return scan_path(root)


if __name__ == "__main__":
    unittest.main()
