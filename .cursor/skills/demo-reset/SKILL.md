---
name: demo-reset
description: Safely restores the two seeded INC-001 RabbitMQ retry hunks and deterministic telemetry without destructive Git commands.
---

# Demo reset

Run `make demo-reset`. The reset uses protected regex baselines to restore only
`RetryCount = 8` and `Delay = TimeSpan.Zero`, regenerates deterministic
telemetry, and confirms the retry-policy check fails as expected. It is
idempotent. Never use `git reset`, `git clean`, or delete this skill.
