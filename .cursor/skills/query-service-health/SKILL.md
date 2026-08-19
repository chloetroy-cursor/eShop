---
name: query-service-health
description: Queries ordering-api error rate, p95, monitor state, and release markers for an absolute incident window. Use for INC-001 or service-health questions.
---

# Query service health

Read-only. Default to `ordering-api` and
`2026-08-18T20:00:00Z`–`2026-08-18T20:30:00Z`; never substitute a rolling
window. Prefer Datadog MCP. Fallback:

```bash
python3 demo/incident/query_telemetry.py health
python3 demo/incident/query_telemetry.py monitor
python3 demo/incident/query_telemetry.py deploy
```

Return service, absolute window, monitor ID/state/time, release/version/time,
post-release error rate and p95, evidence source, and gaps.
