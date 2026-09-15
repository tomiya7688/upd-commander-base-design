import io
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

from upd_commander_checker.report_output import finish_report


class ReportOutputTest(unittest.TestCase):
    def test_parent_creation_failure_returns_tool_error(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            blocker = Path(directory) / "blocker"
            blocker.write_text("file", encoding="utf-8")
            output = blocker / "report.txt"
            stream = io.StringIO()
            with redirect_stdout(stream):
                code = finish_report(["OK"], str(output), 0)

        self.assertEqual(2, code)
        self.assertIn(f"I/O ERROR: failed to write output: {output}", stream.getvalue())
        self.assertNotIn("Traceback", stream.getvalue())

    def test_directory_output_returns_tool_error(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "report"
            output.mkdir()
            stream = io.StringIO()
            with redirect_stdout(stream):
                code = finish_report(["OK"], str(output), 1)

        self.assertEqual(2, code)
        self.assertIn(f"I/O ERROR: failed to write output: {output}", stream.getvalue())


if __name__ == "__main__":
    unittest.main()
