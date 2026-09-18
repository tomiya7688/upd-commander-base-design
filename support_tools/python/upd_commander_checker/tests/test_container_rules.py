import ast
import unittest
from pathlib import Path

from upd_commander_checker.container_rules import check_containers
from upd_commander_checker.models import ModuleInfo


class ContainerRulesTest(unittest.TestCase):
    def test_nested_callable_returns_do_not_trigger_outer_upd302(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def run(self):
        def inner():
            return 1, 2

        async def inner_async():
            return 3, 4

        callback = lambda: (5, 6)

        class Nested:
            def value(self):
                return 7, 8

        return 1
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )

        outer_upd302 = [
            finding
            for finding in findings
            if finding.code == "UPD302" and finding.line == 3
        ]
        self.assertEqual([], outer_upd302)

        nested_upd302 = [finding for finding in findings if finding.code == "UPD302"]
        self.assertEqual(1, len(nested_upd302))

    def test_outer_tuple_return_still_triggers_upd302(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def run(self):
        def inner():
            return 1

        return 1, 2
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )

        upd302 = [finding for finding in findings if finding.code == "UPD302"]
        self.assertEqual(1, len(upd302))

    def test_private_method_is_checked(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def _run(self, left, right, mode):
        return left, right
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )
        self.assertTrue(any(finding.code == "UPD301" for finding in findings))
        self.assertTrue(any(finding.code == "UPD302" for finding in findings))

    def test_default_threshold_allows_two_and_flags_three(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def good(self, left, right):
        pass

    def bad(self, left, right, mode):
        pass
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )
        upd301_lines = [finding.line for finding in findings if finding.code == "UPD301"]
        self.assertEqual([6], upd301_lines)

    def test_custom_threshold_changes_boundary(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def two(self, left, right):
        pass

    def three(self, left, right, mode):
        pass

    def four(self, left, right, mode, extra):
        pass
"""
        )
        module = ModuleInfo(Path("worker.py"), "process", "processing")

        strict = check_containers(tree, module, 1)
        self.assertEqual(
            [3, 6, 9],
            [finding.line for finding in strict if finding.code == "UPD301"],
        )

        relaxed = check_containers(tree, module, 3)
        self.assertEqual(
            [9],
            [finding.line for finding in relaxed if finding.code == "UPD301"],
        )

    def test_variadic_parameters_each_count_as_one_input(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def good(self, left, *values):
        pass

    def bad(self, left, right, *values):
        pass

    def keyword_bad(self, left, right, **options):
        pass
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )
        self.assertEqual(
            [6, 9],
            [finding.line for finding in findings if finding.code == "UPD301"],
        )

    def test_staticmethod_has_no_implicit_receiver(self) -> None:
        tree = ast.parse(
            """
class Worker:
    @staticmethod
    def run(self, left, right):
        pass
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )
        self.assertTrue(any(finding.code == "UPD301" for finding in findings))

    def test_construction_hooks_are_not_checked(self) -> None:
        tree = ast.parse(
            """
class Worker:
    def __new__(cls, left, right):
        return super().__new__(cls)

    def __init__(self, left, right):
        self.value = left + right
"""
        )
        findings = check_containers(
            tree,
            ModuleInfo(Path("worker.py"), "process", "processing"),
        )
        self.assertFalse(any(finding.code == "UPD301" for finding in findings))


if __name__ == "__main__":
    unittest.main()
