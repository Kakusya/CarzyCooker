"""从即时 CLI JSON/stdin 读取 Console entries，只打印有界文本。"""
import json
import sys

from error_message_compactor import compact_error_entries, format_compact_errors


def normalize_entries(payload):
    if isinstance(payload, dict) and "data" in payload:
        if payload.get("success") is not True:
            raise ValueError("CLI Console 查询失败")
        payload = payload["data"]["result"]
    if isinstance(payload, str):
        payload = json.loads(payload)
    if not isinstance(payload, dict) or not isinstance(payload.get("entries"), list):
        raise ValueError("输入缺少当前 Console entries")
    entries = []
    for item in payload["entries"]:
        entries.append(dict(level=item.get("logType", item.get("level", "Log")),
                            message=item.get("message", ""), source=item.get("source", ""),
                            stack=str(item.get("stackTrace", "")).splitlines(), timestamp=item.get("timestampUtc", "")))
    return entries


if __name__ == "__main__":
    try:
        payload = json.load(sys.stdin)
        print(format_compact_errors(compact_error_entries(normalize_entries(payload))))
        result = payload.get("data", {}).get("result", payload) if isinstance(payload, dict) else {}
        if isinstance(result, str):
            result = json.loads(result)
        if result.get("dropped") or result.get("reset"):
            print("覆盖提示：Console 缓冲丢失/游标重置，不能据此断言无错误。")
    except (ValueError, KeyError, TypeError) as error:
        print(f"Console 提取失败：{error}", file=sys.stderr)
        raise SystemExit(1)
