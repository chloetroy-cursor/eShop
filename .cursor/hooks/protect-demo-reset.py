#!/usr/bin/env python3
"""Block deletion of permanent demo-reset assets."""
from __future__ import annotations

import json
import re
import sys

PROTECTED = (
    ".cursor/skills/demo-reset",
    ".cursor/hooks/protect-demo-reset.py",
    ".cursor/hooks.json",
    ".cursor/rules/demo-reset-protected.mdc",
)
PROTECTED_ANCESTORS = (".cursor", ".cursor/skills", ".cursor/hooks", ".cursor/rules")
DELETE = re.compile(r"(?:^|[;&|]\s*)(?:sudo\s+)?(?:git\s+rm|rmdir|rm|trash|unlink)\b", re.I)


def contains_protected(text: str) -> bool:
    normalized = text.replace("\\", "/")
    if any(path in normalized for path in PROTECTED):
        return True
    return any(
        re.search(rf"(?<![\w/.-]){re.escape(path)}(?:[\"']?)(?=\s|$|[,}}])", normalized)
        for path in PROTECTED_ANCESTORS
    )


def deny() -> dict:
    message = "Blocked: demo-reset protection files are permanent. Run `make demo-reset` instead."
    return {"permission": "deny", "user_message": message, "agent_message": message}


def decide(payload: dict) -> dict:
    command = str(payload.get("command") or "")
    if "demo-reset/scripts/reset.py" in command.replace("\\", "/") and not DELETE.search(command):
        return {"permission": "allow"}
    if command and DELETE.search(command) and contains_protected(command):
        return deny()
    tool = str(payload.get("tool_name") or payload.get("tool") or "").lower()
    tool_input = payload.get("tool_input") or payload.get("arguments") or {}
    blob = tool_input if isinstance(tool_input, str) else json.dumps(tool_input)
    if tool in {"delete", "remove"} and contains_protected(blob):
        return deny()
    return {"permission": "allow"}


def main() -> int:
    try:
        payload = json.loads(sys.stdin.read() or "{}")
        answer = decide(payload)
    except (json.JSONDecodeError, TypeError):
        answer = {"permission": "allow"}
    print(json.dumps(answer))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
