---
name: correlate-release
description: Correlates ordering-api incident onset with release metadata and RabbitMQ retry code. Use when assessing v9.1.0-demo causality.
---

# Correlate release

Read-only. Compare the monitor and metric onset with release `v9.1.0-demo` at
`2026-08-18T20:04:00Z`. Read `demo/incident/CHANGELOG.md`, Git history, and:

- `src/EventBusRabbitMQ/EventBusOptions.cs`
- `src/EventBusRabbitMQ/RabbitMQEventBus.cs`

Report onset/deploy ordering, exact retry count and delay, confidence, and
evidence that would falsify release causality. Do not revert or deploy.
