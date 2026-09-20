import unittest
from pathlib import Path

from upd_commander_checker.classifier import classify_import, classify_module


class CommonClassifierTest(unittest.TestCase):
    def test_default_common_and_shared_roots_are_neutral(self) -> None:
        common = classify_module(Path("applications/main/common/contracts/message.py"))
        shared = classify_import("applications.main.shared.contracts.message")

        self.assertEqual("main", common.application)
        self.assertEqual("common", common.layer)
        self.assertEqual("main", shared.application)
        self.assertEqual("common", shared.layer)

    def test_custom_common_root_is_supported(self) -> None:
        module = classify_module(
            Path("applications/main/contracts/message.py"),
            common_roots=("contracts",),
        )
        self.assertEqual("common", module.layer)

    def test_empty_common_roots_disables_common_classification(self) -> None:
        module = classify_module(Path("common/message.py"), common_roots=())
        self.assertIsNone(module.layer)

    def test_innermost_layer_or_common_marker_wins(self) -> None:
        layer = classify_module(Path("common/ui/screen.py"))
        common = classify_module(Path("ui/common/message.py"))
        self.assertEqual("ui", layer.layer)
        self.assertEqual("common", common.layer)


if __name__ == "__main__":
    unittest.main()
