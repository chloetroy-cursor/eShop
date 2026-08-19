#!/usr/bin/env python3
"""Query deterministic fallback telemetry without mutating it."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

DATA = Path(__file__).parent / "generated"


def load(name: str):
    return json.loads((DATA / name).read_text())


def health() -> dict:
    metadata, metrics = load("metadata.json"), load("metrics.json")
    released = metadata["deploy"]["started_at"]
    after = [row for row in metrics if row["timestamp"] >= released]
    return {
        "service": "ordering-api", "window": metadata["window"],
        "monitor_id": metadata["monitor"]["id"], "release": metadata["deploy"],
        "after_release": {
            "error_rate": round(sum(x["errors"] for x in after) / sum(x["requests"] for x in after), 3),
            "p95_ms": max(x["p95_ms"] for x in after),
        },
        "source": "demo/incident/generated/metrics.json",
    }


def request(request_id: str) -> dict:
    metadata = load("metadata.json")
    spans = [x for x in load("traces.json") if x["request_id"] == request_id]
    logs = [x for x in load("logs.json") if x["request_id"] == request_id]
    attempts = sorted((x for x in spans if "attempt" in x), key=lambda x: x["attempt"])
    return {
        "request_id": request_id, "window": metadata["window"],
        "monitor_id": metadata["monitor"]["id"], "release": metadata["deploy"]["version"],
        "spans": spans, "logs": logs, "attempts": len(attempts),
        "backoff_present": any(x["delay_ms"] > 0 for x in attempts),
        "source": "demo/incident/generated/traces.json",
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("health")
    req = sub.add_parser("request")
    req.add_argument("--request-id", required=True)
    sub.add_parser("monitor")
    sub.add_parser("deploy")
    args = parser.parse_args()
    metadata = load("metadata.json")
    values = {
        "health": health,
        "request": lambda: request(args.request_id),
        "monitor": lambda: metadata["monitor"],
        "deploy": lambda: metadata["deploy"],
    }
    print(json.dumps(values[args.command](), indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
