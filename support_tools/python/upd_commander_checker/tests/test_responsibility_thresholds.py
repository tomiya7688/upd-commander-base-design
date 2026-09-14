import ast
import unittest
from pathlib import Path

from upd_commander_checker.models import ModuleInfo
from upd_commander_checker.responsibility_rules import check_responsibilities


class ResponsibilityThresholdTest(unittest.TestCase):
    def _findings(self, source: str):
        module = ModuleInfo(Path("sample.py"), None, None)
        return check_responsibilities(ast.parse(source), module)

    def test_250_lines_is_allowed(self) -> None:
        source = "class Example:\n" + "\n".join("    pass" for _ in range(249))
        self.assertFalse(any(item.code == "UPD401" for item in self._findings(source)))

    def test_251_lines_warns(self) -> None:
        source = "class Example:\n" + "\n".join("    pass" for _ in range(250))
        self.assertTrue(any(item.code == "UPD401" for item in self._findings(source)))

    def test_12_methods_is_allowed(self) -> None:
        methods = "\n".join(f"    def method_{index}(self): pass" for index in range(12))
        source = "class Example:\n" + methods
        self.assertFalse(any(item.code == "UPD401" for item in self._findings(source)))

    def test_13_methods_warns(self) -> None:
        methods = "\n".join(f"    def method_{index}(self): pass" for index in range(13))
        source = "class Example:\n" + methods
        self.assertTrue(any(item.code == "UPD401" for item in self._findings(source)))


if __name__ == "__main__":
    unittest.main()
