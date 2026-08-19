#!/usr/bin/env python3
"""Restore only the protected INC-001 seed hunks and regenerate telemetry."""
from __future__ import annotations

import json
import os
import re
import signal
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASELINE = ROOT / ".cursor/skills/demo-reset/baseline/incident-files.json"
PID_FILE = ROOT / "demo/incident/generated/demo.pid"
ALLOWED = {
    "src/EventBusRabbitMQ/EventBusOptions.cs",
    "src/EventBusRabbitMQ/RabbitMQEventBus.cs",
}


def stop_demo() -> None:
    if not PID_FILE.exists():
        return
    try:
        pid = int(PID_FILE.read_text().strip())
        os.kill(pid, signal.SIGTERM)
        print(f"stopped demo process {pid}")
    except (ProcessLookupError, ValueError):
        pass
    finally:
        PID_FILE.unlink(missing_ok=True)


def restore() -> None:
    spec = json.loads(BASELINE.read_text())
    paths = {item["path"] for item in spec["replacements"]}
    if paths != ALLOWED:
        raise RuntimeError("protected baseline may contain only the two incident files")
    for item in spec["replacements"]:
        path = ROOT / item["path"]
        original = path.read_text()
        updated, count = re.subn(item["pattern"], item["replacement"], original)
        if count != 1:
            raise RuntimeError(f"protected baseline matched {count} times: {item['path']}")
        path.write_text(updated)
        print(f"restored {item['path']}")


def run(relative: str, *, expected: int = 0) -> None:
    result = subprocess.run([sys.executable, str(ROOT / relative)], cwd=ROOT)
    if result.returncode != expected:
        raise RuntimeError(f"{relative} returned {result.returncode}; expected {expected}")


def main() -> int:
    stop_demo()
    restore()
    run("demo/incident/generate_telemetry.py")
    run("demo/incident/check_retry_policy.py", expected=1)
    print("DEMO RESET READY: unsafe INC-001 start restored as expected")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
