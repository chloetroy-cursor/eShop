---
name: trace-request
description: Follows one ordering-api request ID across traces and logs. Use when INC-001 or telemetry names a request ID.
---

# Trace one request

Read-only. Default to `req-order-7f3a0000`. Search Datadog traces/logs inside
the incident's absolute window, or run:

```bash
python3 demo/incident/query_telemetry.py request --request-id req-order-7f3a0000
```

Return root span, ordered publish attempts, durations/errors, matching log,
total attempts, and whether positive backoff exists. Cite request ID, window,
release, monitor, and evidence file.
