#!/usr/bin/env python3
"""Generate deterministic, read-only fallback evidence for INC-001."""
from __future__ import annotations

import json
from datetime import datetime, timedelta, timezone
from pathlib import Path

OUT = Path(__file__).parent / "generated"
START = datetime(2026, 8, 18, 20, 0, tzinfo=timezone.utc)
RELEASE = START + timedelta(minutes=4)
FEATURED = "req-order-7f3a0000"


def stamp(value: datetime) -> str:
    return value.isoformat().replace("+00:00", "Z")


def write(name: str, value: object) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(json.dumps(value, indent=2, sort_keys=True) + "\n")


def main() -> int:
    metrics = []
    for minute in range(30):
        after = minute >= 4
        metrics.append({
            "timestamp": stamp(START + timedelta(minutes=minute)),
            "service": "ordering-api",
            "version": "9.1.0-demo" if after else "9.0.9",
            "requests": 25,
            "errors": (7 + (minute * 3) % 7) if after else 0,
            "error_rate": round((7 + (minute * 3) % 7) / 25, 2) if after else 0.0,
            "p95_ms": 2025 if after else 48,
        })
    attempt_time = RELEASE + timedelta(minutes=3)
    spans = [{
        "request_id": FEATURED, "span_id": "root", "parent_id": None,
        "service": "ordering-api", "resource": "POST /checkout",
        "timestamp": stamp(attempt_time), "duration_ms": 2025, "error": True,
        "version": "9.1.0-demo",
    }]
    for attempt in range(1, 10):
        spans.append({
            "request_id": FEATURED, "span_id": f"publish-{attempt}", "parent_id": "root",
            "service": "ordering-api", "resource": "RabbitMQ publish",
            "timestamp": stamp(attempt_time), "duration_ms": 225, "error": True,
            "attempt": attempt, "delay_ms": 0, "version": "9.1.0-demo",
        })
    logs = [{
        "timestamp": stamp(attempt_time), "service": "ordering-api",
        "request_id": FEATURED, "version": "9.1.0-demo", "status": "error",
        "message": "rabbitmq publish retry exhausted", "attempts": 9,
    }]
    metadata = {
        "incident_id": "INC-001", "source": "deterministic-fallback",
        "window": {"from": stamp(START), "to": stamp(START + timedelta(minutes=30))},
        "featured_request_id": FEATURED,
        "deploy": {"service": "ordering-api", "version": "9.1.0-demo",
                   "marker": "release/v9.1.0-demo", "started_at": stamp(RELEASE)},
        "monitor": {"id": "mon-ordering-publish-errors",
                    "name": "ordering-api checkout publish error rate", "status": "Alert",
                    "triggered_at": stamp(RELEASE + timedelta(minutes=3)),
                    "query": "sum:ordering.checkout.publish.errors{service:ordering-api}.as_rate() > 0.15"},
    }
    write("metrics.json", metrics)
    write("traces.json", spans)
    write("logs.json", logs)
    write("metadata.json", metadata)
    print(f"generated deterministic telemetry in {OUT.relative_to(Path.cwd())}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
