import unittest
from pathlib import Path

from upd_commander_checker.finding import Finding
from upd_commander_checker.gui import _checker_command, _relative_path
from upd_commander_checker.rule_selection import (
    filter_enabled_findings,
    normalize_enabled_rules,
)


class RuleSelectionTest(unittest.TestCase):
    def test_missing_selection_keeps_all_findings(self) -> None:
        findings = [Finding(Path("sample.py"), 1, "UPD101", "message", "error")]
        self.assertEqual(findings, filter_enabled_findings(findings, None))

    def test_empty_selection_disables_all_findings(self) -> None:
        findings = [Finding(Path("sample.py"), 1, "UPD101", "message", "error")]
        self.assertEqual([], filter_enabled_findings(findings, ()))

    def test_selected_rules_filter_findings(self) -> None:
        findings = [
            Finding(Path("first.py"), 1, "UPD101", "message", "error"),
            Finding(Path("second.py"), 2, "UPD202", "message", "warning"),
        ]
        selected = filter_enabled_findings(findings, ("UPD202",))
        self.assertEqual(["UPD202"], [finding.code for finding in selected])

    def test_gui_uses_relative_config_paths(self) -> None:
        base = Path("C:/checker")
        self.assertEqual("../project", _relative_path(base, "C:/project").replace("\\", "/"))

    def test_gui_development_command_uses_module_entry(self) -> None:
        self.assertEqual("-m", _checker_command()[1])

    def test_unsupported_rule_is_rejected(self) -> None:
        with self.assertRaises(ValueError):
            normalize_enabled_rules(["UPD999"])


if __name__ == "__main__":
    unittest.main()
