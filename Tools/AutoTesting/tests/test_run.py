import json
from contextlib import redirect_stderr
import io
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from autotesting.run import (ERROR, FAILED, NOT_RUN, PASS, SKIPPED, PipelineError,
                            Runner, UnityClient, command_result, main, parse_first_json, select_tests, single_flight, verdict)

PROJECT = Path(__file__).resolve().parents[3] / "Unity"


def completed(status="Passed"):
    names = {"Passed": "passed", "Failed": "failed", "Skipped": "skipped", "Inconclusive": "inconclusive"}
    summary = dict(total=1, passed=0, failed=0, skipped=0, inconclusive=0)
    summary[names[status]] = 1
    return dict(status="completed", summary=summary,
                results=[dict(status=status, fullname="Tests.Flow", message="failure reason", stacktrace="Assets/Test.cs:42")])


class FakeClient:
    project = PROJECT

    def __init__(self, terminal=None, busy=False, empty=False, console=None, transient=False, native_busy=False, baseline=None):
        self.terminal = terminal or completed()
        self.busy, self.empty, self.console, self.transient = busy, empty, console, transient
        self.calls = []
        self.started = False
        self.native_busy, self.baseline = native_busy, baseline

    def native_tests_active(self):
        self.calls.append(("native_tests_active",))
        return self.native_busy

    def discover(self):
        self.calls.append(("discover",))

    def command(self, name, *args):
        self.calls.append((name, *args))
        if name == "editor_status":
            return dict(status="ready", compiling=False, playmode="stopped", projectpath=str(PROJECT))
        if name == "test_status":
            if not self.started:
                return dict(status="running" if self.busy else "no_tests")
            if self.transient:
                self.transient = False
                raise PipelineError("domain reload")
            return self.terminal
        if name == "list_tests":
            tests = [] if self.empty else [dict(fullname="Tests.Flow", assembly="Tests", categories=["Smoke"], explicit=False)]
            return dict(count=len(tests), tests=tests)
        if name == "console_status":
            return self.baseline if self.baseline is not None else dict(cursor=5, session="session-a", groundtruth=dict(consoleerrors=4, compilationfailed=False))
        if name == "run_tests":
            self.started = True
            return dict(result="running")
        if name == "console":
            console = dict(entries=[], cursor=5, session="session-a", reset=False, dropped=False, groundtruth=dict(consoleerrors=4, compilationfailed=False))
            if self.console is not None:
                console.update(self.console)
            return console
        raise AssertionError(name)


class TestRunner(unittest.TestCase):
    def execute(self, client, timeout=5):
        output = []
        now = [0]
        runner = Runner(client, output.append, lambda duration: now.__setitem__(0, now[0] + duration), lambda: now[0])
        return runner.execute("playmode", "testName", "Tests.Flow", timeout, 1), output

    def test_success_reads_same_session_errors_after_stopped(self):
        client = FakeClient()
        code, output = self.execute(client)
        self.assertEqual(code, PASS)
        self.assertIn(("console", "--since", "5", "--since_session", "session-a", "--level", "log", "--tail", "2147483647"), client.calls)
        self.assertTrue(any("历史 Console Error 数量：4" in line for line in output))

    def test_failure_keeps_message_and_stack(self):
        code, output = self.execute(FakeClient(completed("Failed")))
        self.assertEqual(code, FAILED)
        self.assertIn("failure reason", output)
        self.assertIn("Assets/Test.cs:42", output)

    def test_empty_never_starts_job(self):
        client = FakeClient(empty=True)
        self.assertEqual(self.execute(client)[0], NOT_RUN)
        self.assertFalse(client.started)

    def test_skipped_and_inconclusive_are_not_passed(self):
        self.assertEqual(self.execute(FakeClient(completed("Skipped")))[0], SKIPPED)
        self.assertEqual(self.execute(FakeClient(completed("Inconclusive")))[0], FAILED)

    def test_running_job_never_cancelled_or_replaced(self):
        client = FakeClient(busy=True)
        with self.assertRaises(PipelineError):
            self.execute(client)
        self.assertFalse(client.started)
        self.assertFalse(any(call[0] in {"cancel_tests", "editor_stop"} for call in client.calls))

    def test_native_utf_busy_blocks_without_pipeline_job(self):
        client = FakeClient(native_busy=True)
        with self.assertRaisesRegex(PipelineError, "原生 UTF"):
            self.execute(client)
        self.assertFalse(client.started)

    def test_timeout_starts_once_and_does_not_cancel(self):
        client = FakeClient(dict(status="running"))
        with self.assertRaisesRegex(PipelineError, "超时"):
            self.execute(client)
        self.assertEqual(sum(call[0] == "run_tests" for call in client.calls), 1)
        self.assertFalse(any(call[0] == "cancel_tests" for call in client.calls))

    def test_transient_poll_failure_recovers_without_new_run(self):
        client = FakeClient(transient=True)
        self.assertEqual(self.execute(client)[0], PASS)
        self.assertEqual(sum(call[0] == "run_tests" for call in client.calls), 1)

    def test_new_console_error_fails(self):
        console = dict(entries=[dict(logtype="Exception", message="boom", stacktrace="Assets/Test.cs:5")], session="session-a")
        self.assertEqual(self.execute(FakeClient(console=console))[0], FAILED)

    def test_reset_or_dropped_console_never_passes(self):
        for change in (dict(reset=True), dict(dropped=True), dict(session="session-b")):
            console = dict(entries=[], session="session-a", **{k: v for k, v in change.items() if k != "session"})
            if "session" in change:
                console["session"] = change["session"]
            self.assertEqual(self.execute(FakeClient(console=console))[0], ERROR)

    def test_wrong_project_rejected_even_when_successful(self):
        envelope = dict(success=True, data=dict(target=dict(projectPath=str(PROJECT.parent / "Other")), result=dict(status="ready")))
        with self.assertRaisesRegex(PipelineError, "工程不符"):
            command_result(envelope, PROJECT)

    def test_discovery_checks_endpoint_when_list_has_no_project_path(self):
        client = UnityClient.__new__(UnityClient)
        client.project = PROJECT
        probe = dict(success=True, data=dict(target=dict(host="127.0.0.1", port=7800, projectPath=str(PROJECT)), result=dict(status="ready")))
        tools = []
        for name, parameters in {"editor_status": [], "test_status": [], "console_status": [],
                                 "console": ["since", "since_session", "tail", "level"], "list_tests": ["mode"], "eval": ["code"],
                                 "run_tests": ["mode", "filter", "filter_type", "async_tests", "timeout"]}.items():
            tools.append(dict(name=name, parameters=[dict(name=parameter) for parameter in parameters]))
        listing = dict(success=True, data=dict(target=dict(host="127.0.0.1", port=7800), tools=tools))
        client.invoke = lambda *args: probe if args[0] == "command" else listing
        client.discover()
        listing["data"]["target"]["port"] = 7801
        with self.assertRaises(PipelineError):
            client.discover()

    def test_nested_error_and_legacy_string_result(self):
        envelope = dict(success=True, data=dict(target=dict(projectPath=str(PROJECT)), result=json.dumps(dict(Success=False, Error="bad"))))
        with self.assertRaisesRegex(PipelineError, "bad"):
            command_result(envelope, PROJECT)

    def test_inconsistent_results_not_accepted(self):
        result = completed()
        result["results"] = []
        with self.assertRaises(PipelineError):
            verdict(result)

    def test_zero_terminal_results_not_passed(self):
        self.assertEqual(verdict(dict(status="completed", summary=dict(total=0, passed=0, failed=0, skipped=0, inconclusive=0), results=[])), NOT_RUN)

    def test_category_is_exact_while_names_and_assemblies_are_partial(self):
        tests = [dict(fullname="Tests.Flow", assembly="Game.Tests", categories=["Smoke.Boot"], explicit=False)]
        self.assertEqual(select_tests(tests, "category", "Smoke"), [])
        self.assertEqual(select_tests(tests, "category", "smoke.boot"), tests)
        self.assertEqual(select_tests(tests, "assembly", "tests"), tests)
        self.assertEqual(select_tests(tests, "testName", "flow"), tests)

    def test_json_with_banner_and_invalid_brace(self):
        self.assertEqual(parse_first_json('banner {invalid}\n{"success": true} trailing'), dict(success=True))

    def test_tail_truncation_without_dropped_is_not_passed(self):
        console = dict(entries=[dict(seq=seq, level="log", message="noise") for seq in range(7, 1007)], cursor=1006)
        self.assertEqual(self.execute(FakeClient(console=console))[0], ERROR)

    def test_native_error_count_increase_without_entry_is_failed(self):
        console = dict(groundtruth=dict(consoleerrors=5, compilationfailed=False))
        self.assertEqual(self.execute(FakeClient(console=console))[0], FAILED)

    def test_unrelated_terminal_test_list_rejected(self):
        result = completed()
        result["results"][0]["fullname"] = "Other.Unauthorized"
        with self.assertRaisesRegex(PipelineError, "名单"):
            self.execute(FakeClient(result))

    def test_null_ground_truth_preflight_blocks(self):
        client = FakeClient(baseline=dict(cursor=5, session="session-a", groundtruth=None))
        with self.assertRaisesRegex(PipelineError, "尚未采样"):
            self.execute(client)
        self.assertFalse(client.started)

    def test_null_ground_truth_or_entries_at_end_never_passes(self):
        for missing in (dict(groundtruth=None), dict(entries=None)):
            self.assertEqual(self.execute(FakeClient(console=missing))[0], ERROR)

    def test_lock_rejects_second_writer_and_releases(self):
        with tempfile.TemporaryDirectory() as root:
            with single_flight(root):
                with self.assertRaises(PipelineError):
                    with single_flight(root):
                        self.fail("second writer accepted")
            self.assertFalse((Path(root) / "Temp/carzycooker-autotesting.lock").exists())

    def test_execution_requires_explicit_mode_before_contacting_editor(self):
        with redirect_stderr(io.StringIO()), self.assertRaises(SystemExit) as raised:
            main(["--run", "--filter", "Tests.Flow"])
        self.assertEqual(raised.exception.code, 2)


if __name__ == "__main__":
    unittest.main()
