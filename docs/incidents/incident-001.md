# INC-001 — ordering-api RabbitMQ publish retry storm

**Status:** Open  
**Service:** `ordering-api`  
**Window:** `2026-08-18T20:00:00Z`–`2026-08-18T20:30:00Z`  
**Release:** `v9.1.0-demo` at `2026-08-18T20:04:00Z`  
**Monitor:** `mon-ordering-publish-errors`  
**Featured request:** `req-order-7f3a0000`

Within three minutes of release, checkout publish errors and p95 latency rise.
The featured trace has nine RabbitMQ publish attempts with no positive delay.
Begin with telemetry, then launch the three incident agents in parallel.

Fallback commands:

```bash
python3 demo/incident/query_telemetry.py health
python3 demo/incident/query_telemetry.py request --request-id req-order-7f3a0000
```

Do not edit retry behavior, deploy, merge, or send Slack/Jira without explicit
human approval. The bounded proposal is `RetryCount = 3` with positive
exponential backoff; it is not pre-approved.
