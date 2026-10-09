#!/usr/bin/env python3
"""发现 → 前置 → 异步运行 → 轮询终态 → Console → 真实结果。

流程适配自参考 AutoTesting，内部按 Pipeline 0.8.0-exp.1 重写。
只输出文本，不写 JSON 证据、搬移结果、清 Console 或自动取消作业。
"""

import argparse
from contextlib import contextmanager
import json
import math
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

PASS, ERROR, FAILED, SKIPPED, NOT_RUN = range(5)
REPO = Path(__file__).resolve().parents[3]
PROJECT = REPO / "Unity"
# 当前 UTF 1.1.33 没有公开 IsTestRunActive；只读源代码中现役的注册表。
# 查询失败/结构改变拒绝执行，不创建测试、回调或 Editor 脚本。
NATIVE_TEST_GUARD = '''
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy;
var holderType = typeof(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.TestRun.TestJobDataHolder");
if (holderType == null) throw new System.InvalidOperationException("Unsupported UTF registry");
var instance = holderType.GetProperty("instance", flags);
var field = holderType.GetField("TestRuns");
if (instance == null || field == null) throw new System.InvalidOperationException("Unsupported UTF fields");
var runs = (System.Collections.IEnumerable)field.GetValue(instance.GetValue(null));
int active = 0;
foreach (var run in runs) {
    var running = run.GetType().GetField("isRunning");
    if (running == null) throw new System.InvalidOperationException("Unsupported UTF run state");
    if ((bool)running.GetValue(run)) active++;
}
return new { active = active };
'''


class PipelineError(RuntimeError):
    pass


def parse_first_json(text):
    """解析第一个完整对象；不依赖日志的成功字样。"""
    decoder = json.JSONDecoder()
    for index, char in enumerate(text):
        if char != "{":
            continue
        try:
            value, _ = decoder.raw_decode(text[index:])
        except json.JSONDecodeError:
            continue
        if isinstance(value, dict):
            return value
    raise PipelineError("CLI 未返回完整 JSON 对象")


def fields(value):
    """CLI 模型与作业状态的字段大小写不一致。"""
    if isinstance(value, dict):
        return {key.lower(): fields(item) for key, item in value.items()}
    if isinstance(value, list):
        return [fields(item) for item in value]
    return value


def same_project(left, right):
    return os.path.normcase(str(Path(left).resolve())) == os.path.normcase(str(Path(right).resolve()))


def command_result(envelope, project):
    payload = fields(envelope)
    if payload.get("success") is not True:
        raise PipelineError(str(payload.get("errors") or payload.get("message") or "CLI 失败"))
    data = payload.get("data", {})
    target = data.get("target", {}).get("projectpath")
    if not target or not same_project(target, project):
        raise PipelineError("返回的 target.projectPath 与当前工程不符")
    result = data.get("result")
    if isinstance(result, str):
        result = fields(parse_first_json(result))
    if not isinstance(result, dict):
        raise PipelineError("命令 result 不是对象")
    if result.get("success") is False or result.get("error"):
        raise PipelineError(str(result.get("error") or result.get("message") or "Pipeline 命令失败"))
    return result


class UnityClient:
    def __init__(self, project=PROJECT):
        self.project = Path(project).resolve()
        self.executable = shutil.which("unity")
        if not self.executable:
            raise PipelineError("找不到 unity CLI")

    def invoke(self, *arguments):
        try:
            proc = subprocess.run(
                [self.executable, *arguments, "--project-path", str(self.project), "--format", "json", "--non-interactive"],
                capture_output=True, text=True, encoding="utf-8", errors="strict", timeout=30,
            )
        except (OSError, UnicodeError, subprocess.TimeoutExpired) as error:
            raise PipelineError(f"CLI 调用失败：{error}") from error
        if proc.returncode:
            raise PipelineError(f"CLI exit={proc.returncode}: {(proc.stderr or proc.stdout).strip()[:3000]}")
        return parse_first_json(proc.stdout)

    def discover(self):
        probe = fields(self.invoke("command", "editor_status"))
        command_result(probe, self.project)
        envelope = fields(self.invoke("list"))
        if envelope.get("success") is not True:
            raise PipelineError("命令发现失败")
        data = envelope.get("data", {})
        # list 的 target 只有 host/port；先核对 editor_status 的工程，
        # 再确认清单来自同一 endpoint，不虚构 list.projectPath 字段。
        target = data.get("target", {})
        expected = probe.get("data", {}).get("target", {})
        if any(target.get(key) is None or target[key] != expected.get(key) for key in ("host", "port")):
            raise PipelineError("命令清单 endpoint 与已核实工程不符")
        tools = {item["name"]: item for item in data.get("tools", [])}
        required = {
            "editor_status": set(), "test_status": set(), "console_status": set(),
            "console": {"since", "since_session", "tail", "level"}, "list_tests": {"mode"},
            "eval": {"code"},
            "run_tests": {"mode", "filter", "filter_type", "async_tests", "timeout"},
        }
        for name, parameters in required.items():
            actual = {item["name"] for item in tools.get(name, {}).get("parameters", [])}
            if name not in tools or not parameters <= actual:
                raise PipelineError(f"当前命令接口不兼容：{name}；请按实际清单适配")

    def command(self, name, *arguments):
        return command_result(self.invoke("command", name, *arguments), self.project)

    def native_tests_active(self):
        response = self.command("eval", "--code", NATIVE_TEST_GUARD)
        value = response.get("result")
        if isinstance(value, str):
            value = fields(parse_first_json(value))
        if not isinstance(value, dict) or type(value.get("active")) is not int or value["active"] < 0:
            raise PipelineError("无法核实原生 UTF 作业；不启动测试")
        return value["active"] > 0


def select_tests(tests, filter_type, value):
    pattern = value.lower()
    def matches(test):
        if filter_type == "assembly":
            values = [test.get("assembly", "")]
        elif filter_type == "category":
            values = test.get("categories", []) or []
            return any(pattern == str(item).lower() for item in values)
        else:
            values = [test.get("fullname", "")]
        return any(pattern in str(item).lower() for item in values)
    return [test for test in tests if not test.get("explicit", False) and matches(test)]


def verdict(result):
    if result.get("status") != "completed":
        raise PipelineError(f"测试未完成：{result.get('status')}: {result.get('message', '')}")
    summary = result.get("summary")
    if not isinstance(summary, dict):
        raise PipelineError("终态缺少 summary")
    counts = [summary.get(name) for name in ("total", "passed", "failed", "skipped", "inconclusive")]
    if any(type(value) is not int or value < 0 for value in counts):
        raise PipelineError("summary 计数缺失或无效")
    total, passed, failed, skipped, inconclusive = counts
    if total != passed + failed + skipped + inconclusive:
        raise PipelineError("summary 计数不一致")
    if not total:
        return NOT_RUN
    results = result.get("results")
    if not isinstance(results, list) or len(results) != total:
        raise PipelineError("终态缺少完整逐测试结果")
    actual = {"passed": 0, "failed": 0, "skipped": 0, "inconclusive": 0}
    for item in results:
        status = str(item.get("status", "")).lower()
        if status not in actual:
            raise PipelineError(f"未知测试状态：{status}")
        actual[status] += 1
    if any(actual[name] != summary[name] for name in actual):
        raise PipelineError("逐测试结果与 summary 不符")
    if failed or inconclusive:
        return FAILED
    if skipped:
        return SKIPPED
    return PASS


@contextmanager
def single_flight(project):
    """本工具独占锁；不保存作业结果，异常退出的残锁不得自动删除。"""
    directory = Path(project) / "Temp"
    directory.mkdir(parents=True, exist_ok=True)
    lock = directory / "carzycooker-autotesting.lock"
    try:
        descriptor = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError as error:
        raise PipelineError(f"测试编排锁已存在：{lock}；先核实持有进程与实际作业，不自动删除") from error
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8") as stream:
            stream.write(f"pid={os.getpid()}\n")
        yield
    finally:
        lock.unlink()


class Runner:
    def __init__(self, client, emit=print, sleep=time.sleep, clock=time.monotonic):
        self.client, self.emit, self.sleep, self.clock = client, emit, sleep, clock

    def ready(self):
        status = self.client.command("editor_status")
        if not same_project(status.get("projectpath", ""), self.client.project):
            raise PipelineError("editor_status.projectPath 与当前工程不符")
        if status.get("status") != "ready" or status.get("compiling") or status.get("domainreloadinprogress"):
            raise PipelineError("Editor 未 ready 或正在编译/域重载")
        if status.get("playmode") != "stopped":
            raise PipelineError("Play 尚未停止；本工具不自动停止用户会话")
        test = self.client.command("test_status")
        if test.get("status") not in {"no_tests", "completed", "cancelled", "error"}:
            raise PipelineError("已有运行/未知测试状态；本工具不自动取消")
        if self.client.native_tests_active():
            raise PipelineError("已有原生 UTF 作业；不启动测试或取消用户运行")

    def listing(self, mode):
        self.client.discover()
        self.ready()
        result = self.client.command("list_tests", "--mode", mode)
        tests = result.get("tests")
        if not isinstance(tests, list) or type(result.get("count")) is not int or result["count"] != len(tests):
            raise PipelineError("测试发现结果缺失或计数不一致")
        self.emit(f"Discovered: mode={mode}, count={len(tests)}")
        for item in tests:
            self.emit(f"{item.get('fullname')} | {item.get('assembly')} | explicit={item.get('explicit')}")
        return tests

    def poll(self, timeout, interval):
        deadline = self.clock() + timeout
        last_error = ""
        while self.clock() < deadline:
            try:
                status = self.client.command("test_status")
            except PipelineError as error:
                last_error = str(error)
                self.sleep(min(interval, max(0, deadline - self.clock())))
                continue
            state = status.get("status")
            if state == "completed":
                return status
            if state != "running":
                raise PipelineError(f"异常测试终态：{state}: {status.get('message', '')}")
            self.sleep(min(interval, max(0, deadline - self.clock())))
        raise PipelineError(f"测试轮询超时；作业可能仍运行，禁止启动新作业；最后错误：{last_error}")

    def stopped(self, interval):
        deadline = self.clock() + 60
        while self.clock() < deadline:
            try:
                status = self.client.command("editor_status")
                if status.get("playmode") == "stopped" and not status.get("compiling") and not status.get("domainreloadinprogress"):
                    return
            except PipelineError:
                pass
            self.sleep(interval)
        raise PipelineError("测试已终态但 Player/域重载未停止；不启动下一轮")

    def execute(self, mode, filter_type, value, timeout, interval):
        tests = self.listing(mode)
        selected = select_tests(tests, filter_type, value)
        if not selected:
            self.emit("NotRun: 无匹配的非 Explicit 测试")
            return NOT_RUN
        self.ready()
        baseline = self.client.command("console_status")
        ground = baseline.get("groundtruth")
        if not isinstance(ground, dict):
            raise PipelineError("Console groundTruth 尚未采样；等待当前 Editor 就绪，不启动测试")
        if ground.get("compilationfailed"):
            raise PipelineError("Editor groundTruth 报告编译失败")
        if not isinstance(baseline.get("session"), str) or type(baseline.get("cursor")) is not int:
            raise PipelineError("Console 缺少 session/cursor")
        self.emit(f"历史 Console Error 数量：{ground.get('consoleerrors', 'unknown')}（不清除）")
        started = self.client.command("run_tests", "--mode", mode, "--filter_type", filter_type,
                                      "--filter", value, "--async_tests", "--timeout", str(timeout))
        if started.get("result") != "running":
            raise PipelineError("未获得异步运行确认，禁止重发 run_tests")
        result = self.poll(timeout, interval)
        self.emit(f"终态 summary: {result.get('summary')}")
        for item in result.get("results", []):
            self.emit(f"{item.get('status')} | {item.get('fullname')}")
            if str(item.get("status", "")).lower() != "passed":
                self.emit(str(item.get("message", "")))
                self.emit(str(item.get("stacktrace", "")))
        code = verdict(result)
        if code != NOT_RUN:
            expected = [test.get("fullname") for test in selected]
            observed = [test.get("fullname") for test in result["results"]]
            if not all(isinstance(name, str) and name for name in expected + observed) or sorted(expected) != sorted(observed):
                raise PipelineError("终态测试名单与已发现的授权范围不一致，不能证明本轮完成")
        self.stopped(interval)
        self.sleep(2)
        console = self.client.command("console", "--since", str(baseline["cursor"]),
                                      "--since_session", baseline["session"], "--level", "log", "--tail", "2147483647")
        entries = console.get("entries")
        errors = [item for item in (entries if isinstance(entries, list) else [])
                  if str(item.get("logtype", item.get("level", ""))).lower() in {"error", "exception", "assert"}]
        for item in errors:
            self.emit(f"Console {item.get('logtype', item.get('level'))}: {item.get('message')}\n{item.get('stacktrace', '')}")
        ending_ground = console.get("groundtruth")
        if errors or (isinstance(ending_ground, dict) and ending_ground.get("compilationfailed")):
            code = FAILED
        # 当前接口每个捕获条目递增 seq，level=log 覆盖全部级别。
        # tail 截断不一定置 dropped，不能只依赖 dropped/reset 两个标志。
        cursor = console.get("cursor")
        contiguous = (type(cursor) is int and cursor >= baseline["cursor"] and isinstance(entries, list)
                      and len(entries) == cursor - baseline["cursor"]
                      and all(item.get("seq") == baseline["cursor"] + index for index, item in enumerate(entries, 1)))
        ground_valid = isinstance(ending_ground, dict) and type(ending_ground.get("consoleerrors")) is int
        native_errors_increased = (ground_valid and type(ground.get("consoleerrors")) is int
                                   and ending_ground["consoleerrors"] > ground["consoleerrors"])
        if native_errors_increased:
            self.emit("Editor 原生 Console Error 计数增加；即使捕获条目缺失也判为失败")
            code = FAILED
        incomplete = (console.get("reset") or console.get("dropped") or console.get("session") != baseline["session"]
                      or not contiguous or not ground_valid
                      or type(console.get("reset")) is not bool or type(console.get("dropped")) is not bool)
        if incomplete:
            self.emit("Console 覆盖不完整：会话变化、缓冲丢失或条目缺失；不声明完整通过")
            if code != FAILED:
                code = ERROR
        self.emit(f"原始 Pipeline 作业输出保留在 {self.client.project / 'Temp/pipeline_test_status.json'}；未复制或改写")
        self.emit(f"Conclusion: {dict(enumerate(['Passed', 'Error', 'Failed', 'Skipped', 'NotRun']))[code]}")
        return code


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mode", choices=("editor", "playmode"))
    parser.add_argument("--run", action="store_true", help="执行已授权的明确过滤范围；默认只发现")
    parser.add_argument("--filter", default="")
    parser.add_argument("--filter_type", choices=("testName", "assembly", "category"), default="testName")
    parser.add_argument("--timeout", type=int, default=600)
    parser.add_argument("--poll-interval", type=float, default=3)
    args = parser.parse_args(argv)
    if args.timeout <= 0 or not math.isfinite(args.poll_interval) or not 0 < args.poll_interval <= 30:
        parser.error("timeout 必须大于零，poll-interval 必须在 (0,30] 内")
    if args.run and not args.filter.strip():
        parser.error("--run 必须给 --filter；先发现真实测试，不自动跑全工程")
    if args.run and args.mode is None:
        parser.error("--run 必须显式给 --mode，不能猜测运行范围")
    args.mode = args.mode or "editor"
    try:
        runner = Runner(UnityClient())
        if args.run:
            with single_flight(PROJECT):
                return runner.execute(args.mode, args.filter_type, args.filter, args.timeout, args.poll_interval)
        runner.listing(args.mode)
        print("NotRun: 仅发现，未执行任何测试")
        return PASS  # 命令成功仅说明发现成功；不是测试通过
    except PipelineError as error:
        print(f"Error: {error}", file=sys.stderr)
        return ERROR
    except KeyboardInterrupt:
        print("Interrupted: 作业可能仍运行；未取消，先核实 test_status", file=sys.stderr)
        return ERROR


if __name__ == "__main__":
    raise SystemExit(main())
