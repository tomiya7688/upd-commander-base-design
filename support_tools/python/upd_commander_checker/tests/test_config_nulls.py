import json
import os
import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.config import ConfigError, load_config


class ConfigNullTest(unittest.TestCase):
    def test_null_and_invalid_typed_fields_are_rejected(self) -> None:
        cases = (
            ({"input": None}, "input"),
            ({"output": None}, "output"),
            ({"ignore": None}, "ignore"),
            ({"ignore": [None]}, "ignore"),
            ({"warnings_as_errors": None}, "warnings_as_errors"),
            ({"enabled_rules": None}, "enabled_rules"),
            ({"input": 1}, "input"),
            ({"warnings_as_errors": "true"}, "warnings_as_errors"),
            ({"upd301_max_inputs": None}, "upd301_max_inputs"),
            ({"upd301_max_inputs": True}, "upd301_max_inputs"),
            ({"upd301_max_inputs": "2"}, "upd301_max_inputs"),
            ({"upd301_max_inputs": 2.0}, "upd301_max_inputs"),
            ({"upd301_max_inputs": 0}, "upd301_max_inputs"),
            ({"upd301_max_inputs": -1}, "upd301_max_inputs"),
        )
        for payload, field in cases:
            with self.subTest(field=field, payload=payload):
                with self.assertRaisesRegex(ConfigError, f"invalid config field: {field}"):
                    self._load(json.dumps(payload))

    def test_null_root_is_rejected(self) -> None:
        with self.assertRaisesRegex(ConfigError, "invalid config:"):
            self._load("null")

    def _load(self, content: str):
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config_dir = root / "config"
            config_dir.mkdir()
            (config_dir / "path.json").write_text(content, encoding="utf-8")
            try:
                os.chdir(root)
                return load_config()
            finally:
                os.chdir(original)


if __name__ == "__main__":
    unittest.main()
