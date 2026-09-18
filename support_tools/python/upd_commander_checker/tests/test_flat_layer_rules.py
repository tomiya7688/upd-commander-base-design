import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class FlatLayerRuleTests(unittest.TestCase):
    def _write_files(self, root: Path, direct: int, nested: int, excluded: int = 0) -> None:
        layer = root / "process"
        layer.mkdir(parents=True, exist_ok=True)
        for index in range(direct):
            (layer / f"direct_{index}.py").write_text(f"value_{index} = {index}\n", encoding="utf-8")
        nested_dir = layer / "group"
        for index in range(nested):
            nested_dir.mkdir(parents=True, exist_ok=True)
            (nested_dir / f"nested_{index}.py").write_text(f"nested_{index} = {index}\n", encoding="utf-8")
        generated = layer / "generated"
        for index in range(excluded):
            generated.mkdir(parents=True, exist_ok=True)
            (generated / f"generated_{index}.py").write_text(f"generated_{index} = {index}\n", encoding="utf-8")

    def test_default_boundary(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_files(root, 9, 3)
            self.assertFalse(any(item.code == "UPD405" for item in scan_path(root)))

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_files(root, 10, 2)
            findings = [item for item in scan_path(root) if item.code == "UPD405"]
            self.assertEqual(1, len(findings))
            self.assertEqual(Path("process"), findings[0].path)
            self.assertEqual("attention", findings[0].severity)

    def test_small_and_generated_heavy_do_not_trigger(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_files(root, 8, 0)
            self.assertFalse(any(item.code == "UPD405" for item in scan_path(root)))

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_files(root, 5, 0, 20)
            self.assertFalse(any(item.code == "UPD405" for item in scan_path(root)))

    def test_custom_thresholds(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_files(root, 6, 0)
            findings = scan_path(root, (), 2, 6, 80)
            self.assertTrue(any(item.code == "UPD405" for item in findings))


if __name__ == "__main__":
    unittest.main()
