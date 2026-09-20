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
                        "common_roots": ["contracts", "Shared", "contracts"],
                        "enabled_rules": ["UPD101", "UPD202"],
                        "upd301_max_inputs": 3,
                        "flat_layer_min_files": 14,
                        "flat_layer_min_direct_percent": 90,
                        "model_group_min_items": 4,
                        "model_group_min_occurrences": 3,
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
            self.assertEqual(("contracts", "shared"), config.common_roots)
            self.assertEqual(("UPD101", "UPD202"), config.enabled_rules)
            self.assertEqual(3, config.upd301_max_inputs)
            self.assertEqual(14, config.flat_layer_min_files)
            self.assertEqual(90, config.flat_layer_min_direct_percent)
            self.assertEqual(4, config.model_group_min_items)
            self.assertEqual(3, config.model_group_min_occurrences)

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
            self.assertEqual(("common", "shared"), config.common_roots)
            self.assertEqual(2, config.upd301_max_inputs)
            self.assertEqual(12, config.flat_layer_min_files)
            self.assertEqual(80, config.flat_layer_min_direct_percent)
            self.assertEqual(3, config.model_group_min_items)
            self.assertEqual(2, config.model_group_min_occurrences)

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

    def test_invalid_flat_layer_thresholds_raise_config_error(self) -> None:
        invalid_values = (
            '{"flat_layer_min_files": 0}',
            '{"flat_layer_min_files": true}',
            '{"flat_layer_min_direct_percent": 0}',
            '{"flat_layer_min_direct_percent": 101}',
            '{"flat_layer_min_direct_percent": 80.0}',
        )
        original = Path.cwd()
        for content in invalid_values:
            with self.subTest(content=content), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                (root / "config").mkdir()
                (root / "config" / "path.json").write_text(content, encoding="utf-8")
                try:
                    os.chdir(root)
                    with self.assertRaises(ConfigError):
                        load_config()
                finally:
                    os.chdir(original)

    def test_invalid_common_roots_raise_config_error(self) -> None:
        invalid_values = (
            '{"common_roots": null}',
            '{"common_roots": "common"}',
            '{"common_roots": [1]}',
            '{"common_roots": [""]}',
            '{"common_roots": ["."]}',
            '{"common_roots": [".."]}',
            '{"common_roots": ["ui"]}',
            '{"common_roots": ["process"]}',
            '{"common_roots": ["data"]}',
            '{"common_roots": ["nested/common"]}',
            '{"common_roots": ["nested\\\\common"]}',
        )
        original = Path.cwd()
        for content in invalid_values:
            with self.subTest(content=content), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                (root / "config").mkdir()
                (root / "config" / "path.json").write_text(content, encoding="utf-8")
                try:
                    os.chdir(root)
                    with self.assertRaises(ConfigError):
                        load_config()
                finally:
                    os.chdir(original)

    def test_empty_common_roots_disables_recognition(self) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "config").mkdir()
            (root / "config" / "path.json").write_text(
                '{"common_roots": []}', encoding="utf-8"
            )
            try:
                os.chdir(root)
                config = load_config()
            finally:
                os.chdir(original)
            self.assertEqual((), config.common_roots)


if __name__ == "__main__":
    unittest.main()
