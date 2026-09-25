import hashlib
import json
import re
import unittest
import unicodedata
from pathlib import Path, PurePosixPath


FIXTURE_PATH = (
    Path(__file__).resolve().parents[4]
    / "specification"
    / "baseline-fingerprint-fixtures.json"
)


def fingerprint(finding: dict[str, object]) -> str:
    rule = str(finding["rule"]).upper()
    if re.fullmatch(r"UPD[0-9]{3,}", rule) is None:
        raise ValueError("invalid rule code")

    path = unicodedata.normalize("NFC", str(finding["path"]).replace("\\", "/"))
    parsed_path = PurePosixPath(path)
    if (
        parsed_path.is_absolute()
        or re.match(r"^[A-Za-z]:/", path)
        or path.startswith("//")
        or ".." in parsed_path.parts
    ):
        raise ValueError("path must be repository-relative")
    path = "/".join(part for part in parsed_path.parts if part not in {"", "."})
    if not path:
        raise ValueError("path must not be empty")
    symbol = unicodedata.normalize("NFC", str(finding.get("symbol", "")))
    context = unicodedata.normalize("NFC", str(finding["context"]))
    fields = ("upd-finding-fingerprint-v1", rule, path, symbol, context)
    if not context or any("\0" in field for field in fields):
        raise ValueError("context must be non-empty and fields cannot contain NUL")
    payload = "\0".join(fields).encode("utf-8")
    return "sha256:" + hashlib.sha256(payload).hexdigest()


class BaselineFixtureContractTests(unittest.TestCase):
    def test_golden_fingerprints_and_comparison_expectations(self) -> None:
        fixture = json.loads(FIXTURE_PATH.read_text(encoding="utf-8"))
        vectors = {item["name"]: item for item in fixture["vectors"]}
        fingerprints = {}

        for name, vector in vectors.items():
            actual = fingerprint(vector["finding"])
            self.assertEqual(vector["expected_fingerprint"], actual, name)
            fingerprints[name] = actual

        for comparison in fixture["comparisons"]:
            left = fingerprints[comparison["left"]]
            right = fingerprints[comparison["right"]]
            self.assertEqual(
                comparison["expected_equal"],
                left == right,
                f"{comparison['left']} vs {comparison['right']}",
            )

    def test_rejects_ambiguous_identity_fields(self) -> None:
        with self.assertRaises(ValueError):
            fingerprint(
                {
                    "rule": "UPD101",
                    "path": "../outside.cs",
                    "symbol": "",
                    "context": "target=data",
                }
            )
        with self.assertRaises(ValueError):
            fingerprint(
                {
                    "rule": "UPD101",
                    "path": "C:/outside.cs",
                    "symbol": "",
                    "context": "target=data",
                }
            )
        with self.assertRaises(ValueError):
            fingerprint(
                {
                    "rule": "UPD101",
                    "path": "src/file.cs",
                    "symbol": "",
                    "context": "",
                }
            )


if __name__ == "__main__":
    unittest.main()
