from __future__ import annotations

from dataclasses import dataclass, field
from html import unescape
import codecs
import json
import os
import re
from typing import Iterable, Sequence


FAILURE_LEVELS = {"error", "exception", "assert"}
DEFAULT_FRAME_EXCLUDE_PATTERNS = (
    r"^UnityEngine\.Debug",
    r"^UnityEngine\.StackTraceUtility",
    r"^System\.Reflection",
    r"^UnityEditor\.EditorApplication",
    r"^UnityEditor\.EditorAssemblies",
)
ERROR_MESSAGE_FRAME_EXCLUDE_PATTERNS = DEFAULT_FRAME_EXCLUDE_PATTERNS + (
    r"\./Library/PackageCache/",
    r"\(at Library/PackageCache/",
)
_ENTRY_HEADER_RE = re.compile(
    r"^(?:error:\s*)?Unity Console (?P<level>Error|Exception|Assert):\s*(?P<message>.*)$",
    re.IGNORECASE,
)
_SOURCE_RE = re.compile(r"Assets[/\\].+?\.cs:\d+", re.IGNORECASE)
_AT_SOURCE_RE = re.compile(r"\(at (?P<source>(?:Assets|Share\.SourceGenerator)[/\\].+?\.cs:\d+)\)", re.IGNORECASE)
_ABSOLUTE_SOURCE_RE = re.compile(r"[A-Za-z]:[/\\].+?(?P<source>(?:Assets|Share\.SourceGenerator)[/\\].+?\.cs):(?P<line>\d+)", re.IGNORECASE)
_HREF_SOURCE_RE = re.compile(
    r"<a\s+[^>]*href=\"(?P<path>[^\"]+\.cs)\"[^>]*line=\"(?P<line>\d+)\"[^>]*>",
    re.IGNORECASE,
)


@dataclass(slots=True)
class ErrorEntry:
    level: str = "Error"
    message: str = ""
    source: str = ""
    stack: list[str] = field(default_factory=list)
    timestamp: str = ""


@dataclass(slots=True)
class CompactError:
    level: str
    count: int
    message: str
    source: str = ""
    stack: list[str] = field(default_factory=list)


@dataclass(slots=True)
class ErrorCompactionOptions:
    max_entries: int = 5
    max_stack_frames: int = 5
    max_message_lines: int = 3
    dedupe: str = "message_source"
    include_patterns: Sequence[str] = ()
    exclude_patterns: Sequence[str] = ()
    frame_exclude_patterns: Sequence[str] = ERROR_MESSAGE_FRAME_EXCLUDE_PATTERNS
    project_stack_only: bool = True


def compact_error_message(raw_error: str, options: ErrorCompactionOptions | None = None) -> str:
    """Compact a raw Unity-style error string into bounded human-readable text."""
    entries = parse_error_message(raw_error)
    compact = compact_error_entries(entries, options)
    return format_compact_errors(compact)


def parse_error_message(raw_error: str, default_level: str = "Error") -> list[ErrorEntry]:
    """Parse one or more pasted Unity Console error blocks into reducer entries."""
    text = _extract_json_error_message(raw_error)
    text = (text or "").replace("\r\n", "\n").replace("\r", "\n")
    blocks: list[list[str]] = []
    current: list[str] = []

    for raw_line in text.split("\n"):
        line = raw_line.rstrip()
        if _ENTRY_HEADER_RE.match(line) and current:
            blocks.append(current)
            current = [line]
        elif line or current:
            current.append(line)

    if current:
        blocks.append(current)

    if not blocks and text.strip():
        blocks = [[line.rstrip() for line in text.split("\n")]]

    return [_parse_block(block, default_level) for block in blocks if any(line.strip() for line in block)]


def compact_error_entries(
    entries: Iterable[ErrorEntry | dict],
    options: ErrorCompactionOptions | None = None,
) -> list[CompactError]:
    options = options or ErrorCompactionOptions()
    include = _compile(options.include_patterns)
    exclude = _compile(options.exclude_patterns)
    frame_exclude = _compile(options.frame_exclude_patterns)
    results: list[CompactError] = []
    by_key: dict[str, CompactError] = {}

    for raw_entry in entries or ():
        entry = _coerce_entry(raw_entry)
        if _normalize_level(entry.level).lower() not in FAILURE_LEVELS:
            continue

        searchable = "\n".join((entry.level, entry.message, entry.source, "\n".join(entry.stack)))
        if include and not any(pattern.search(searchable) for pattern in include):
            continue
        if any(pattern.search(searchable) for pattern in exclude):
            continue

        message = _compact_message(entry.message, options.max_message_lines)
        stack: list[str] = []
        for frame in entry.stack:
            trimmed = _normalize_frame(frame.strip())
            if not trimmed or any(pattern.search(trimmed) for pattern in frame_exclude):
                continue
            if options.project_stack_only and not _frame_has_project_source(trimmed):
                continue
            stack.append(trimmed)
            if len(stack) >= max(0, options.max_stack_frames):
                break

        source = _select_source(entry.source, stack)
        key = _build_key(options.dedupe, message, source, stack)
        if options.dedupe != "none" and key in by_key:
            by_key[key].count += 1
            continue

        compact = CompactError(
            level=_normalize_level(entry.level),
            count=1,
            message=message,
            source=source,
            stack=stack,
        )
        results.append(compact)
        if options.dedupe != "none":
            by_key[key] = compact

        if len(results) >= max(1, options.max_entries) and options.dedupe == "none":
            break

    return results[: max(1, options.max_entries)]


def format_compact_errors(errors: Iterable[CompactError | dict]) -> str:
    lines: list[str] = []
    for raw_error in errors:
        error = _coerce_compact_error(raw_error)
        header = f"[{error.level}] count={error.count}"
        if error.source:
            header += f" source={error.source}"
        lines.append(header)
        lines.append(error.message or "<empty message>")
        lines.extend(f"  {frame}" for frame in error.stack)
    return "\n".join(lines).rstrip() if lines else "No Error/Exception/Assert entries found."


def compact_errors_to_json(errors: Iterable[CompactError | dict]) -> str:
    payload = []
    for raw_error in errors:
        error = _coerce_compact_error(raw_error)
        payload.append(
            {
                "level": error.level,
                "count": error.count,
                "message": error.message,
                "source": error.source,
                "stack": error.stack,
            }
        )
    return json.dumps(payload, ensure_ascii=False, indent=2)


def high_frequency_candidates(errors: Iterable[CompactError | dict], minimum_count: int = 3) -> list[dict[str, object]]:
    """Return review-only candidate signatures; never alters reducer rules at runtime."""
    candidates: list[dict[str, object]] = []
    for raw_error in errors:
        error = _coerce_compact_error(raw_error)
        if error.count < minimum_count:
            continue
        candidates.append({
            "signature": (error.message.split("\n", 1)[0], error.source),
            "count": error.count,
            "suggestedPattern": re.escape(error.message.split("\n", 1)[0]),
            "requiresReview": True,
        })
    return candidates


def _parse_block(block: list[str], default_level: str) -> ErrorEntry:
    first = block[0].strip() if block else ""
    header = _ENTRY_HEADER_RE.match(first)
    level = header.group("level") if header else default_level
    first_message = header.group("message") if header else first
    message_lines: list[str] = [first_message] if first_message else []
    stack: list[str] = []
    in_stack = False
    pending_frame = ""

    for line in block[1:]:
        stripped = line.strip()
        if not in_stack and _looks_like_stack_frame(stripped):
            in_stack = True
        if in_stack or _extract_source(stripped):
            source = _extract_source(stripped)
            if source and pending_frame and not _extract_source(pending_frame):
                stack.append(f"{pending_frame} ({source})")
                pending_frame = ""
            elif source:
                stack.append(stripped)
                pending_frame = ""
            else:
                if pending_frame:
                    stack.append(pending_frame)
                pending_frame = stripped
        elif stripped:
            message_lines.append(stripped)

    if pending_frame:
        stack.append(pending_frame)

    source = ""
    for frame in stack:
        source = _extract_source(frame)
        if source:
            break

    return ErrorEntry(level=level, message="\n".join(message_lines).strip(), source=source, stack=stack)


def _looks_like_stack_frame(line: str) -> bool:
    return bool(
        line
        and (
            " at " in line
            or " in Assets/" in line
            or "\\Assets\\" in line
            or "<a " in line
            or re.match(r"^\s+at\s+", line)
            or re.match(r"^[A-Za-z0-9_\.<>`\[\]+]+[:.]\w+", line)
        )
    )


def _extract_json_error_message(raw_error: str) -> str:
    text = raw_error or ""
    for candidate in _json_candidates(text):
        try:
            payload = json.loads(candidate)
        except json.JSONDecodeError:
            continue
        if isinstance(payload, str):
            return _extract_json_error_message(payload)
        if isinstance(payload, dict) and isinstance(payload.get("error_msg"), str):
            return payload["error_msg"]
    return text


def _json_candidates(text: str) -> list[str]:
    stripped = text.strip()
    if not stripped:
        return []
    candidates = [stripped]
    if stripped.startswith('"') and stripped.endswith('"'):
        try:
            decoded = json.loads(stripped)
            if isinstance(decoded, str):
                candidates.append(decoded.strip())
        except json.JSONDecodeError:
            pass
    try:
        decoded = codecs.decode(stripped, "unicode_escape")
        if decoded != stripped:
            candidates.append(decoded.strip())
    except Exception:
        pass
    if not stripped.startswith("{"):
        start = stripped.find("{")
        end = stripped.rfind("}")
        if start >= 0 and end > start:
            candidates.append(stripped[start:end + 1])
    unique = []
    for candidate in candidates:
        if candidate and candidate not in unique:
            unique.append(candidate)
    return unique


def _normalize_slashes(path: str) -> str:
    return path.replace("\\", "/")


def _to_project_relative_source(path: str, line: str | None = None) -> str:
    normalized = _normalize_slashes(unescape(path)).strip()
    for marker in ("/Assets/", "/Share.SourceGenerator/"):
        index = normalized.find(marker)
        if index >= 0:
            normalized = normalized[index + 1:]
            break
    if line and not normalized.endswith(f":{line}"):
        normalized = f"{normalized}:{line}"
    return normalized


def _extract_source(text: str) -> str:
    line = unescape(text or "")
    href_match = _HREF_SOURCE_RE.search(line)
    if href_match:
        return _to_project_relative_source(href_match.group("path"), href_match.group("line"))
    absolute_match = _ABSOLUTE_SOURCE_RE.search(line)
    if absolute_match:
        return _to_project_relative_source(absolute_match.group("source"), absolute_match.group("line"))
    at_match = _AT_SOURCE_RE.search(line)
    if at_match:
        return _to_project_relative_source(at_match.group("source"))
    source_match = _SOURCE_RE.search(line)
    if source_match:
        return _to_project_relative_source(source_match.group(0))
    return ""


def _frame_has_project_source(frame: str) -> bool:
    return bool(_extract_source(frame))


def _normalize_frame(frame: str) -> str:
    source = _extract_source(frame)
    if not source:
        return unescape(frame).strip()
    text = re.sub(r"<a\s+[^>]*>.*?</a>", source, frame, flags=re.IGNORECASE).strip()
    text = _normalize_slashes(unescape(text))
    text = re.sub(r"[A-Za-z]:/.+?((?:Assets|Share\.SourceGenerator)/.+?\.cs:\d+)", r"\1", text, flags=re.IGNORECASE)
    return text


def _compile(patterns: Sequence[str]) -> list[re.Pattern[str]]:
    return [re.compile(pattern, re.IGNORECASE) for pattern in patterns if pattern]


def _normalize_level(level: str) -> str:
    value = (level or "Error").strip().lower()
    if value == "exception":
        return "Exception"
    if value == "assert":
        return "Assert"
    return "Error"


def _compact_message(message: str, max_lines: int) -> str:
    lines = [line.strip() for line in (message or "").replace("\r\n", "\n").split("\n")]
    useful = [line for line in lines if line]
    return "\n".join(useful[: max(1, max_lines)]) if useful else "<empty message>"


def _select_source(source: str, stack: Sequence[str]) -> str:
    selected = _extract_source(source)
    if selected:
        return selected
    if source and source.strip():
        return source.strip()
    for frame in stack:
        selected = _extract_source(frame)
        if selected:
            return selected
    return ""


def _build_key(dedupe: str, message: str, source: str, stack: Sequence[str]) -> str:
    first_line = (message or "").split("\n", 1)[0]
    if dedupe == "message":
        return first_line
    if dedupe == "none":
        return f"none:{id(stack)}"
    first_frame = stack[0] if stack else ""
    return f"{first_line}|{source or first_frame}"


def _coerce_entry(raw_entry: ErrorEntry | dict) -> ErrorEntry:
    if isinstance(raw_entry, ErrorEntry):
        return raw_entry
    return ErrorEntry(
        level=str(raw_entry.get("level", "Error")),
        message=str(raw_entry.get("message", "")),
        source=str(raw_entry.get("source", "")),
        stack=[str(frame) for frame in raw_entry.get("stack", [])],
        timestamp=str(raw_entry.get("timestamp", "")),
    )


def _coerce_compact_error(raw_error: CompactError | dict) -> CompactError:
    if isinstance(raw_error, CompactError):
        return raw_error
    return CompactError(
        level=str(raw_error.get("level", "Error")),
        count=int(raw_error.get("count", 1)),
        message=str(raw_error.get("message", "")),
        source=str(raw_error.get("source", "")),
        stack=[str(frame) for frame in raw_error.get("stack", [])],
    )
