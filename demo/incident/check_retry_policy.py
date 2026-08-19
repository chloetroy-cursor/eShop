#!/usr/bin/env python3
"""Fail while the intentionally unsafe incident seed remains."""
from __future__ import annotations

import re
from pathlib import Path
from typing import Optional

ROOT = Path(__file__).resolve().parents[2]


def inspect_policy(options: Optional[str] = None, bus: Optional[str] = None) -> tuple[int, str, bool]:
    options = (ROOT / "src/EventBusRabbitMQ/EventBusOptions.cs").read_text() if options is None else options
    bus = (ROOT / "src/EventBusRabbitMQ/RabbitMQEventBus.cs").read_text() if bus is None else bus
    count_match = re.search(r"RetryCount \{ get; set; \} = (\d+);", options)
    delay_match = re.search(r"Delay = ([^,\n]+)", bus)
    if not count_match or not delay_match:
        raise RuntimeError("retry policy shape changed; inspect manually")
    count = int(count_match.group(1))
    delay = delay_match.group(1).strip()
    exponential = "BackoffType = DelayBackoffType.Exponential" in bus
    return count, delay, exponential


def is_safe(count: int, delay: str, exponential: bool) -> bool:
    return count == 3 and delay != "TimeSpan.Zero" and exponential


def main() -> int:
    count, delay, exponential = inspect_policy()
    safe = is_safe(count, delay, exponential)
    print(f"RetryCount={count}; Delay={delay}; exponential={exponential}")
    if safe:
        print("PASS: bounded retries with positive exponential backoff")
        return 0
    print("EXPECTED FAIL: seeded INC-001 policy has excessive immediate retries")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
