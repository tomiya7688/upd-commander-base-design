import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class DependencyImportTest(unittest.TestCase):
    def test_from_import_matches_absolute_import_layer_meaning(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            ui = root / "applications" / "settings" / "ui"
            ui.mkdir(parents=True)
            self._write(root / "applications" / "settings" / "data" / "storage.py")
            (ui / "absolute.py").write_text(
                "import applications.settings.data.storage\n",
                encoding="utf-8",
            )
            (ui / "from_import.py").write_text(
                "from applications.settings.data import storage\n",
                encoding="utf-8",
            )

            findings = scan_path(root)
            violation_paths = {item.path.name for item in findings if item.code == "UPD101"}
            self.assertIn("absolute.py", violation_paths)
            self.assertIn("from_import.py", violation_paths)

    def test_from_import_alias_uses_original_target(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "settings" / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            self._write(root / "applications" / "settings" / "data" / "storage.py")
            path.write_text(
                "from applications.settings.data import storage as harmless_name\n",
                encoding="utf-8",
            )

            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_from_import_name_contributes_role_and_layer(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "settings" / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            self._write(root / "applications" / "settings" / "data_processing.py")
            path.write_text(
                "from applications.settings import data_processing\n",
                encoding="utf-8",
            )

            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_relative_import_detects_same_application_layer_violation(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "apps" / "product" / "applications" / "settings" / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            self._write(
                root
                / "apps"
                / "product"
                / "applications"
                / "settings"
                / "data"
                / "storage.py"
            )
            path.write_text("from ..data import storage\n", encoding="utf-8")

            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_relative_import_uses_nested_application_boundary(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "apps" / "product" / "applications" / "settings" / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            self._write(
                root
                / "apps"
                / "product"
                / "applications"
                / "profile"
                / "process"
                / "profile_processing.py"
            )
            path.write_text(
                "from ...profile.process import profile_processing\n",
                encoding="utf-8",
            )

            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_star_import_uses_base_module(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "settings" / "ui" / "screen.py"
            path.parent.mkdir(parents=True)
            self._write(root / "applications" / "settings" / "data" / "__init__.py")
            path.write_text("from applications.settings.data import *\n", encoding="utf-8")

            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD101" for item in findings))

    @staticmethod
    def _write(path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("# internal module\n", encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
