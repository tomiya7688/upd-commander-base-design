import io
import os
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from unittest.mock import patch

from upd_commander_checker.cli import main
from upd_commander_checker.config_model import CheckerConfig
from upd_commander_checker.finding import Finding
from upd_commander_checker.gate_policy import (
    apply_severity_overrides,
    exception_reason,
    normalize_fail_on,
    normalize_gate_exceptions,
    normalize_severity_overrides,
    parse_fail_on_argument,
    parse_severity_override_argument,
    should_fail,
)


class GatePolicyTest(unittest.TestCase):
    def test_normalizes_severities_and_rule_overrides(self) -> None:
        self.assertEqual(("error", "warning"), normalize_fail_on(["ERROR", "warning", "error"]))
        self.assertEqual((), parse_fail_on_argument(""))
        self.assertEqual(("UPD101", "warning"), parse_severity_override_argument("upd101=WARNING"))
        self.assertEqual(( ("UPD101", "attention"), ), normalize_severity_overrides({"upd101": "Attention"}))

    def test_rejects_invalid_gate_policy(self) -> None:
        for value in (None, "error", ["fatal"], [1]):
            with self.subTest(value=value), self.assertRaises(ValueError):
                normalize_fail_on(value)
        for value in (None, [], {"bad": "error"}, {"UPD101": "fatal"}):
            with self.subTest(value=value), self.assertRaises(ValueError):
                normalize_severity_overrides(value)
        with self.assertRaises(ValueError):
            parse_severity_override_argument("UPD101 warning")

    def test_override_changes_effective_severity_but_keeps_finding(self) -> None:
        finding = Finding(Path("src/a.py"), 5, "UPD203", "direct I/O", "error")
        effective = apply_severity_overrides([finding], {"UPD203": "warning"})
        self.assertEqual(1, len(effective))
        self.assertEqual("error", finding.severity)
        self.assertEqual("warning", effective[0].severity)
        self.assertFalse(should_fail(effective, ("error",)))
        self.assertTrue(should_fail(effective, ("warning",)))

    def test_gate_exceptions_match_rule_path_and_optional_line(self) -> None:
        finding = Finding(Path("src/a.py"), 5, "UPD203", "direct I/O", "error")
        exceptions = normalize_gate_exceptions(
            [
                {"rule": "upd203", "path": "src/a.py", "line": 5, "reason": "approved"},
                {"rule": "UPD203", "path": "src/a.py", "reason": "file allowance"},
                {"rule": "UPD203", "path": "src/b.py", "reason": "file allowance"},
            ]
        )
        self.assertEqual("approved", exception_reason(finding, "src/a.py", exceptions))
        other_line = Finding(Path("src/a.py"), 6, "UPD203", "direct I/O", "error")
        self.assertEqual("file allowance", exception_reason(other_line, "src/a.py", exceptions))
        self.assertTrue(should_fail([finding], ("error",)))

    def test_rejects_invalid_gate_exceptions(self) -> None:
        invalid_values = (
            None,
            "src/a.py",
            [{"rule": "bad", "path": "src/a.py", "reason": "approved"}],
            [{"rule": "UPD203", "path": "../a.py", "reason": "approved"}],
            [{"rule": "UPD203", "path": "src\\a.py", "reason": "approved"}],
            [{"rule": "UPD203", "path": "src/*.py", "reason": "approved"}],
            [{"rule": "UPD203", "path": "src/a.py", "reason": "  "}],
            [{"rule": "UPD203", "path": "src/a.py", "reason": "line\nbreak"}],
            [{"rule": "UPD203", "path": "src/a.py", "line": True, "reason": "approved"}],
            [{"rule": "UPD203", "path": "src/a.py", "line": 0, "reason": "approved"}],
            [{"rule": "UPD203", "path": "src/a.py", "reason": "approved", "extra": 1}],
        )
        for value in invalid_values:
            with self.subTest(value=value), self.assertRaises(ValueError):
                normalize_gate_exceptions(value)

    def test_cli_override_and_explicit_gate_control_exit_not_findings(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory)
            source = target / "sample.py"
            source.write_text("pass\n", encoding="utf-8")
            finding = Finding(source, 1, "UPD203", "direct I/O", "error")
            output = io.StringIO()
            with patch("sys.argv", ["checker", "--fail-on", "warning", "--severity-override", "UPD203=warning", str(target)]), patch(
                "upd_commander_checker.cli.scan_path", return_value=[finding]
            ), redirect_stdout(output):
                self.assertEqual(1, main())
            self.assertIn("W UPD203", output.getvalue())
            self.assertIn("FAIL e=0 w=1 a=0", output.getvalue())

    def test_cli_exception_keeps_finding_and_counts_but_allows_gate(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory)
            source = target / "src" / "sample.py"
            source.parent.mkdir()
            source.write_text("pass\n", encoding="utf-8")
            finding = Finding(source, 1, "UPD203", "direct I/O", "error")
            config = CheckerConfig(
                fail_on=("error",),
                gate_exceptions=(
                    normalize_gate_exceptions(
                        [{"rule": "UPD203", "path": "src/sample.py", "line": 1, "reason": "approved boundary"}]
                    )[0],
                ),
            )
            output = io.StringIO()
            with patch("sys.argv", ["checker", str(target)]), patch(
                "upd_commander_checker.cli.load_config", return_value=config
            ), patch("upd_commander_checker.cli.scan_path", return_value=[finding]), redirect_stdout(output):
                self.assertEqual(0, main(), output.getvalue())
            self.assertIn("E UPD203 src/sample.py:1 direct I/O [gate exception: approved boundary]", output.getvalue())
            self.assertIn("OK", output.getvalue())

            output = io.StringIO()
            with patch("sys.argv", ["checker", "--fail-on", "", "--severity-override", "UPD203=warning", str(target)]), patch(
                "upd_commander_checker.cli.scan_path", return_value=[finding]
            ), redirect_stdout(output):
                self.assertEqual(0, main())
            self.assertIn("W UPD203", output.getvalue())
            self.assertIn("OK w=1 a=0", output.getvalue())

            output = io.StringIO()
            with patch("sys.argv", ["checker", "--warnings-as-errors", "--fail-on", "", str(target)]), patch(
                "upd_commander_checker.cli.scan_path", return_value=[finding]
            ), redirect_stdout(output):
                self.assertEqual(0, main())
            self.assertIn("E UPD203", output.getvalue())
            self.assertIn("OK", output.getvalue())

            warning = Finding(Path("src/b.py"), 3, "UPD401", "large unit", "warning")
            output = io.StringIO()
            with patch("sys.argv", ["checker", "--warnings-as-errors", str(target)]), patch(
                "upd_commander_checker.cli.load_config", return_value=CheckerConfig()
            ), patch("upd_commander_checker.cli.scan_path", return_value=[warning]), redirect_stdout(output):
                self.assertEqual(1, main())
            self.assertIn("W UPD401", output.getvalue())
            self.assertIn("FAIL e=0 w=1 a=0", output.getvalue())


if __name__ == "__main__":
    unittest.main()
