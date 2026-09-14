import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.classifier import classify_import, classify_module
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

    def test_scan_root_prevents_parent_layer_leak(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "data" / "project"
            path = root / "ui" / "screen_processing.py"
            path.parent.mkdir(parents=True)
            path.write_text("from data.storage import load\n", encoding="utf-8")
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_cross_application_internal_import_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "main" / "ui" / "screen_processing.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "from applications.settings.process.settings_processing import run\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_cross_application_messenger_import_is_allowed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "applications" / "main" / "process" / "main_commander.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "from applications.settings.process.settings_messenger import send\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD102" for item in findings))

    def test_data_commander_to_data_commander_is_warning(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "data" / "save_commander.py"
            path.parent.mkdir(parents=True)
            path.write_text("from data.cache_commander import run\n", encoding="utf-8")
            findings = scan_path(root)
            warning = next(item for item in findings if item.code == "UPD103")
            self.assertEqual("warning", warning.severity)

    def test_checker_package_name_does_not_make_helpers_commander(self) -> None:
        path = Path("support_tools/python/upd_commander_checker/src/upd_commander_checker/commander_rules.py")
        module = classify_module(path)
        self.assertIsNone(module.role)

    def test_inline_ignore_suppresses_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "fast_commander.py"
            path.parent.mkdir()
            path.write_text(
                "value = left + right  # upd: ignore UPD202 - performance\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD202" for item in findings))

    def test_ignore_file_suppresses_specific_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "fast_commander.py"
            path.parent.mkdir()
            path.write_text("value = left + right\n", encoding="utf-8")
            (root / ".updcommanderignore").write_text(
                "UPD202 process/fast_commander.py # performance\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD202" for item in findings))

    def test_clean_module_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "battle_processing.py"
            path.parent.mkdir()
            path.write_text(
                "def calculate(value: int) -> int:\n    return value + 1\n",
                encoding="utf-8",
            )
            findings = scan_path(root)
            self.assertEqual([], findings)


class NestedApplicationClassifierTest(unittest.TestCase):
    def test_nested_application_internal_import_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "apps" / "product" / "applications" / "settings" / "ui" / "screen_processing.py"
            path.parent.mkdir(parents=True)
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
        layer, role, application = classify_import(
            "apps.product.ui.commander.applications.settings.data.storage"
        )
        self.assertEqual("settings", application)
        self.assertEqual("data", layer)
        self.assertIsNone(role)


if __name__ == "__main__":
    unittest.main()
