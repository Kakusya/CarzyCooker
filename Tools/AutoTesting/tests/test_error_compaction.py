from pathlib import Path
import runpy
import sys
import unittest

SCRIPTS = Path(__file__).resolve().parents[3] / ".agents/skills/unity-error-extraction/scripts"
sys.path.insert(0, str(SCRIPTS))
from compact_console import normalize_entries
from error_message_compactor import compact_error_entries


class TestErrorCompaction(unittest.TestCase):
    def test_reference_regressions(self):
        cases = runpy.run_path(str(SCRIPTS / "test_error_message_compactor.py"))
        for name, function in cases.items():
            if name.startswith("test_") and callable(function):
                with self.subTest(name=name):
                    function()

    def test_pipeline_stack_and_count_preserved(self):
        item = dict(logType="Exception", message="same error", stackTrace="ET.Client.Test.Run() (at Assets/Test.cs:9)")
        entries = normalize_entries(dict(entries=[item, item]))
        result = compact_error_entries(entries)
        self.assertEqual(result[0].count, 2)
        self.assertEqual(result[0].source, "Assets/Test.cs:9")

    def test_failed_cli_does_not_report_empty_errors(self):
        with self.assertRaises(ValueError):
            normalize_entries(dict(success=False, data=dict(result=dict(entries=[]))))


if __name__ == "__main__":
    unittest.main()
