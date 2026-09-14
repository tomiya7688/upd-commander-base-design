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


if __name__ == "__main__":
    unittest.main()
