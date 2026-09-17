import os
import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.config import ConfigError, load_config


class ConfigNullTest(unittest.TestCase):
    def test_null_and_invalid_typed_fields_are_rejected(self) -> None:
        cases = {
            '{"input": null}': "input",
            '{"output": null}': "output",
            '{"ignore": null}': "ignore",
            '{"ignore": [null]}': "ignore",
            '{"warnings_as_errors": null}': "warnings_as_errors",
            '{"enabled_rules": null}': "enabled_rules",
            '{"input": 1}': "input",
            '{"warnings_as_errors": "true"}': "warnings_as_errors",
        }
        for content, field in cases.items():
            with self.subTest(field=field, content=content):
                with self.assertRaisesRegex(ConfigError, f"invalid config field: {field}"):
                    self._load_content(content)

    def test_null_root_is_rejected(self) -> None:
        with self.assertRaisesRegex(ConfigError, "invalid config:"):
            self._load_content("null")

    @staticmethod
    def _load_content(content: str) -> None:
        original = Path.cwd()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config_directory = root / "config"
            config_directory.mkdir()
            (config_directory / "path.json").write_text(content, encoding="utf-8")
            try:
                os.chdir(root)
                load_config()
            finally:
                os.chdir(original)


if __name__ == "__main__":
    unittest.main()
