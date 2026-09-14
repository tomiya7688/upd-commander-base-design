import json
import os
import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.config import ConfigError, load_config


class ConfigTest(unittest.TestCase):
    def test_path_json_is_loaded_from_current_directory(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config_dir = root / "config"
            config_dir.mkdir()
            (config_dir / "path.json").write_text(
                json.dumps(
                    {
                        "input": "project",
                        "output": "reports/check.txt",
                        "ignore": ["generated/**"],
                        "warnings_as_errors": True,
                        "enabled_rules": ["UPD101", "UPD202"],
                    }
                ),
                encoding="utf-8",
            )
            try:
                os.chdir(root)
                config = load_config()
            finally:
                os.chdir(original)

            self.assertEqual(str((root / "project").resolve()), config.input_path)
            self.assertEqual(str((root / "reports" / "check.txt").resolve()), config.output_path)
            self.assertEqual(("generated/**",), config.ignore)
            self.assertTrue(config.warnings_as_errors)
            self.assertEqual(("UPD101", "UPD202"), config.enabled_rules)

    def test_missing_enabled_rules_means_all_rules(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "config").mkdir()
            (root / "config" / "path.json").write_text("{}", encoding="utf-8")
            try:
                os.chdir(root)
                config = load_config()
            finally:
                os.chdir(original)
            self.assertIsNone(config.enabled_rules)

    def test_empty_enabled_rules_means_no_rules(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "config").mkdir()
            (root / "config" / "path.json").write_text(
                '{"enabled_rules": []}', encoding="utf-8"
            )
            try:
                os.chdir(root)
                config = load_config()
            finally:
                os.chdir(original)
            self.assertEqual((), config.enabled_rules)

    def test_invalid_json_raises_config_error(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config_dir = root / "config"
            config_dir.mkdir()
            (config_dir / "path.json").write_text("{ invalid", encoding="utf-8")
            try:
                os.chdir(root)
                with self.assertRaises(ConfigError):
                    load_config()
            finally:
                os.chdir(original)

    def test_invalid_field_type_raises_config_error(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config_dir = root / "config"
            config_dir.mkdir()
            (config_dir / "path.json").write_text('{"ignore": [1]}', encoding="utf-8")
            try:
                os.chdir(root)
                with self.assertRaises(ConfigError):
                    load_config()
            finally:
                os.chdir(original)

    def test_unknown_enabled_rule_raises_config_error(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "config").mkdir()
            (root / "config" / "path.json").write_text(
                '{"enabled_rules": ["UPD999"]}', encoding="utf-8"
            )
            try:
                os.chdir(root)
                with self.assertRaises(ConfigError):
                    load_config()
            finally:
                os.chdir(original)


if __name__ == "__main__":
    unittest.main()
