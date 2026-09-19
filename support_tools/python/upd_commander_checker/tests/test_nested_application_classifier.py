import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.classifier import classify_import, classify_module
from upd_commander_checker.scanner import scan_path


class NestedApplicationClassifierTest(unittest.TestCase):
    def test_nested_application_internal_import_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "apps" / "product" / "applications" / "settings" / "ui" / "screen_processing.py"
            target = (
                root
                / "apps"
                / "product"
                / "applications"
                / "profile"
                / "process"
                / "profile_processing.py"
            )
            path.parent.mkdir(parents=True)
            target.parent.mkdir(parents=True)
            target.write_text("def run():\n    return None\n", encoding="utf-8")
            path.write_text(
                "from apps.product.applications.profile.process.profile_processing import run\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_nearest_application_marker_wins(self) -> None:
        path = Path("apps/project/applications/main/process/main_commander.py")
        module = classify_module(path)
        self.assertEqual("main", module.application)
        self.assertEqual("process", module.layer)
        self.assertEqual("commander", module.role)

    def test_nested_import_uses_nearest_application_scope(self) -> None:
        module = classify_import(
            "apps.product.ui.commander.applications.settings.data.storage"
        )
        self.assertEqual("settings", module.application)
        self.assertEqual("data", module.layer)
        self.assertIsNone(role)


if __name__ == "__main__":
    unittest.main()
