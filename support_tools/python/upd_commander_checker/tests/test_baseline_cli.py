import io
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest.mock import patch

from upd_commander_checker.cli import main
from upd_commander_checker.config_model import CheckerConfig


class BaselineCliTests(unittest.TestCase):
    def test_new_scope_does_not_fail_existing_findings_and_requires_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "ui").mkdir()
            (root / "data").mkdir()
            (root / "ui" / "screen_processing.py").write_text(
                "from data.storage import load\n", encoding="utf-8"
            )
            (root / "data" / "storage.py").write_text(
                "def load():\n    return None\n", encoding="utf-8"
            )
            baseline = root / "baseline.json"
            self._run(root, ["--write-baseline", str(baseline)])
            self.assertEqual(1, self.last_exit_code)

            output = self._run(
                root, ["--baseline", str(baseline), "--fail-on-scope", "new"]
            )
            self.assertEqual(0, self.last_exit_code)
            self.assertIn("EXISTING E UPD101", output)
            self.assertIn("OK", output)

            (root / "ui" / "settings_processing.py").write_text(
                "from data.storage import load\n", encoding="utf-8"
            )
            new_output = self._run(
                root, ["--baseline", str(baseline), "--fail-on", "error", "--fail-on-scope", "new"]
            )
            self.assertEqual(1, self.last_exit_code)
            self.assertIn("EXISTING E UPD101", new_output)
            self.assertIn("NEW E UPD101", new_output)

            warning_output = self._run(
                root,
                [
                    "--baseline",
                    str(baseline),
                    "--fail-on",
                    "warning",
                    "--severity-override",
                    "UPD101=warning",
                    "--fail-on-scope",
                    "new",
                ],
            )
            self.assertEqual(1, self.last_exit_code)
            self.assertIn("NEW W UPD101", warning_output)

            self._run(root, ["--fail-on-scope", "new"])
            self.assertEqual(2, self.last_exit_code)

    def test_write_then_compare_reports_existing_and_resolved(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            ui = root / "ui"
            data = root / "data"
            ui.mkdir()
            data.mkdir()
            target_file = ui / "screen_processing.py"
            target_file.write_text("from data.storage import load\n", encoding="utf-8")
            (data / "storage.py").write_text("def load():\n    return None\n", encoding="utf-8")
            baseline_path = root / "baseline.json"

            write_output = self._run(root, ["--write-baseline", str(baseline_path)])
            self.assertEqual(1, self.last_exit_code)
            self.assertTrue(baseline_path.exists())
            self.assertIn("E UPD101", write_output)

            target_file.write_text("# inserted line\nfrom data.storage import load\n", encoding="utf-8")
            existing_output = self._run(root, ["--baseline", str(baseline_path)])
            self.assertEqual(1, self.last_exit_code)
            self.assertIn("EXISTING E UPD101", existing_output)
            self.assertNotIn("RESOLVED", existing_output)

            target_file.write_text("def screen():\n    return None\n", encoding="utf-8")
            resolved_output = self._run(root, ["--baseline", str(baseline_path)])
            self.assertEqual(0, self.last_exit_code)
            self.assertIn("RESOLVED E UPD101", resolved_output)

    def test_rejects_corrupt_baseline_with_tool_error(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            bad_baseline = root / "baseline.json"
            bad_baseline.write_text("{ broken", encoding="utf-8")
            output = self._run(root, ["--baseline", str(bad_baseline)])

        self.assertEqual(2, self.last_exit_code)
        self.assertIn("BASELINE ERROR:", output)
        self.assertNotIn("Traceback", output)

    def _run(self, root: Path, options: list[str]) -> str:
        arguments = ["upd-commander-check", str(root), *options]
        stream = io.StringIO()
        with patch("sys.argv", arguments), patch(
            "upd_commander_checker.cli.load_config",
            return_value=CheckerConfig(input_path=str(root)),
        ), redirect_stdout(stream):
            self.last_exit_code = main()
        return stream.getvalue()


if __name__ == "__main__":
    unittest.main()
