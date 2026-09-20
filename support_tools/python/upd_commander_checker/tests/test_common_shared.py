import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class CommonSharedPythonTests(unittest.TestCase):
    def _write(self, root: Path, fixture: tuple[str, str]) -> None:
        relative, content = fixture
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content or "value = 1\n", encoding="utf-8")

    def test_layers_may_depend_on_common(self) -> None:
        for layer in ("ui", "process", "data"):
            with self.subTest(layer=layer), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                self._write(root, ("common/contracts/message.py", ""))
                self._write(
                    root,
                    (f"{layer}/consumer.py", "from common.contracts.message import value\n"),
                )
                findings = scan_path(root)
                self.assertFalse(any(item.code == "UPD101" for item in findings))

    def test_common_must_not_depend_on_layer_specific_implementation(self) -> None:
        for layer in ("ui", "process", "data"):
            with self.subTest(layer=layer), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                self._write(root, (f"{layer}/implementation.py", ""))
                self._write(
                    root,
                    ("common/contracts/message.py", f"from {layer}.implementation import value\n"),
                )
                findings = scan_path(root)
                self.assertTrue(any(item.code == "UPD101" for item in findings))

    def test_common_to_common_is_allowed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write(root, ("common/value.py", ""))
            self._write(
                root,
                ("shared/contracts/message.py", "from common.value import value\n"),
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code == "UPD101" for item in findings))

    def test_application_local_common_is_internal_to_application(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write(
                root,
                "applications/settings/common/internal_value.py",
            )
            self._write(
                root,
                (
                    "applications/main/process/main_processing.py",
                    "from applications.settings.common.internal_value import value\n",
                ),
            )
            findings = scan_path(root)
            self.assertTrue(any(item.code == "UPD102" for item in findings))

    def test_product_common_contract_is_available_to_application(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write(root, ("common/contracts/message.py", ""))
            self._write(
                root,
                (
                    "applications/main/process/main_processing.py",
                    "from common.contracts.message import value\n",
                ),
            )
            findings = scan_path(root)
            self.assertFalse(any(item.code in {"UPD101", "UPD102"} for item in findings))

    def test_custom_common_root_is_used_by_dependency_analysis(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write(root, ("contracts/message.py", ""))
            self._write(root, ("ui/screen.py", "from contracts.message import value\n"))
            findings = scan_path(root, common_roots=("contracts",))
            self.assertFalse(any(item.code == "UPD101" for item in findings))

    def test_empty_common_roots_disable_common_dependency_rule(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write(root, "process/implementation.py")
            self._write(
                root,
                ("common/message.py", "from process.implementation import value\n"),
            )
            findings = scan_path(root, common_roots=())
            self.assertFalse(any(item.code == "UPD101" for item in findings))


if __name__ == "__main__":
    unittest.main()
