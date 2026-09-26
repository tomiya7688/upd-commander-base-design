import json
from pathlib import Path
import tempfile
import unittest

from upd_commander_checker.baseline import (
    BaselineError,
    build_baseline,
    compare_findings,
    finding_fingerprint,
    load_baseline,
    validate_baseline,
    write_baseline,
)


def finding(
    *,
    path: str = "src/ui/screen.cs",
    symbol: str = "Ui.Screen.Run",
    context: str = "target=data.storage",
    line: int = 12,
    rule: str = "UPD101",
) -> dict[str, object]:
    return {
        "rule": rule,
        "path": path,
        "symbol": symbol,
        "context": context,
        "severity": "error",
        "line": line,
        "message": "UI must not depend on Data",
    }


class BaselineTests(unittest.TestCase):
    def test_fingerprint_matches_shared_golden_vector(self) -> None:
        self.assertEqual(
            "sha256:ea890f274ed082af81eded22f8f1f869348dc62665221bff76200c3543e86cd1",
            finding_fingerprint("UPD101", "src/ui/screen.cs", "Ui.Screen.Run", "target=data.storage"),
        )

    def test_write_and_load_round_trip(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "nested" / "baseline.json"
            write_baseline(path, [finding()])
            loaded = load_baseline(path)

        self.assertEqual(1, loaded["schema_version"])
        self.assertEqual(1, loaded["fingerprint_version"])
        self.assertEqual(1, len(loaded["findings"]))
        self.assertEqual("src/ui/screen.cs", loaded["findings"][0]["path"])

    def test_baseline_output_is_deterministic(self) -> None:
        one, two = finding(path="src/ui/one.cs"), finding(path="src/ui/two.cs")
        self.assertEqual(build_baseline([one, two]), build_baseline([two, one]))

    def test_classifies_new_existing_and_resolved(self) -> None:
        existing = finding()
        resolved = finding(path="src/data/old.cs", context="target=legacy")
        baseline = build_baseline([existing, resolved])
        current = [finding(line=47), finding(path="src/ui/new.cs", context="target=data.backup")]

        result = compare_findings(current, baseline)

        self.assertEqual(["NEW"], [item["status"] for item in result["new"]])
        self.assertEqual(["EXISTING"], [item["status"] for item in result["existing"]])
        self.assertEqual(["RESOLVED"], [item["status"] for item in result["resolved"]])
        self.assertEqual(47, result["existing"][0]["line"])

    def test_line_severity_and_message_do_not_change_identity(self) -> None:
        baseline = build_baseline([finding()])
        changed = finding(line=80)
        changed["severity"] = "warning"
        changed["message"] = "Updated display text"

        result = compare_findings([changed], baseline)

        self.assertEqual(1, len(result["existing"]))

    def test_rejects_duplicate_identities_in_scan(self) -> None:
        with self.assertRaisesRegex(BaselineError, "duplicate Finding identity"):
            build_baseline([finding(), finding(line=22)])

    def test_rejects_malformed_versions_fingerprint_and_duplicate_entries(self) -> None:
        baseline = build_baseline([finding()])
        for invalid in (
            {**baseline, "schema_version": 2},
            {**baseline, "fingerprint_version": 2},
            {**baseline, "findings": [{**baseline["findings"][0], "fingerprint": "sha256:bad"}]},
            {**baseline, "findings": baseline["findings"] * 2},
        ):
            with self.subTest(invalid=invalid):
                with self.assertRaises(BaselineError):
                    validate_baseline(invalid)

    def test_accepts_unknown_forward_compatible_properties(self) -> None:
        baseline = build_baseline([finding()])
        baseline["future_metadata"] = {"ignored": True}
        baseline["findings"][0]["future_field"] = "ignored"

        self.assertEqual(1, len(validate_baseline(baseline)["findings"]))

    def test_rejects_invalid_identity_and_bad_json(self) -> None:
        with self.assertRaises(BaselineError):
            finding_fingerprint("UPD101", "../outside.cs", "", "context")
        with self.assertRaises(BaselineError):
            build_baseline([{**finding(), "context": ""}])
        with tempfile.TemporaryDirectory() as temporary:
            bad_json = Path(temporary) / "bad.json"
            bad_json.write_text("{ invalid", encoding="utf-8")
            with self.assertRaisesRegex(BaselineError, "cannot read baseline"):
                load_baseline(bad_json)


if __name__ == "__main__":
    unittest.main()
