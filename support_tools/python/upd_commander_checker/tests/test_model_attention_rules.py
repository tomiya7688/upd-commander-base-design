import tempfile
import unittest
from pathlib import Path

from upd_commander_checker.scanner import scan_path


class ModelAttentionRuleTests(unittest.TestCase):
    def test_repeated_parameter_group_triggers(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "sample.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "def first(id, name, email):\n    return id\n\n"
                "def second(id, name, email):\n    return name\n",
                encoding="utf-8",
            )
            findings = [item for item in scan_path(root) if item.code == "UPD406"]
            self.assertEqual(1, len(findings))
            self.assertIn("items=id,name,email", findings[0].message)
            self.assertIn("occurrences=2", findings[0].message)
            self.assertIn("kind=parameters", findings[0].message)

    def test_two_items_order_and_cross_layer_do_not_trigger(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            process = root / "process"
            process.mkdir(parents=True)
            (process / "two.py").write_text(
                "def first(id, name):\n    return id\n\n"
                "def second(id, name):\n    return name\n",
                encoding="utf-8",
            )
            (process / "order.py").write_text(
                "def first(id, name, email):\n    return id\n\n"
                "def second(email, name, id):\n    return email\n",
                encoding="utf-8",
            )
            ui = root / "ui"
            ui.mkdir()
            (ui / "cross.py").write_text(
                "def only(id, name, email):\n    return id\n",
                encoding="utf-8",
            )
            self.assertFalse(any(item.code == "UPD406" for item in scan_path(root)))

    def test_tuple_parallel_and_rule_specific_ignore(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            process = root / "process"
            process.mkdir(parents=True)
            (process / "tuple_case.py").write_text(
                "def first(x, y, z):\n    return (x, y, z)\n\n"
                "def second(a, b, c):\n    return (x, y, z)\n",
                encoding="utf-8",
            )
            findings = [item for item in scan_path(root) if item.code == "UPD406"]
            self.assertTrue(any("kind=tuple" in item.message for item in findings))

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            process = root / "process"
            process.mkdir(parents=True)
            (process / "parallel.py").write_text(
                "def one(xs, ys, zs, i):\n    value = xs[i] + ys[i] + zs[i]\n    return value\n\n"
                "def two(xs, ys, zs, i):\n    value = xs[i] + ys[i] + zs[i]\n    return value\n",
                encoding="utf-8",
            )
            (root / ".updcommanderignore").write_text(
                "UPD406 process/parallel.py\n",
                encoding="utf-8",
            )
            self.assertFalse(any(item.code == "UPD406" for item in scan_path(root)))

    def test_custom_thresholds(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "process" / "sample.py"
            path.parent.mkdir(parents=True)
            path.write_text(
                "def first(id, name, email):\n    return id\n\n"
                "def second(id, name, email):\n    return name\n",
                encoding="utf-8",
            )
            findings = scan_path(root, (), 2, 12, 80, 4, 2)
            self.assertFalse(any(item.code == "UPD406" for item in findings))


if __name__ == "__main__":
    unittest.main()
