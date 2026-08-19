# Agent guidance

This remains the upstream .NET eShop reference application. Preserve application behavior and the existing `.cursor/skills/migration-*`, `migrate-to-rust`, and `migration-validate` assets.

For incident work, start at `docs/incidents/incident-001.md` and use `incident-response`. Begin with ordering-api telemetry and the fixed incident window, not a random source file. Datadog and generated fallback telemetry are read-only evidence.

Every incident claim needs the applicable request ID, absolute time window, monitor ID, release/version marker, and file citation. Do not change RabbitMQ retry behavior, deploy, merge, or send Slack/Jira without explicit human approval. External actions remain draft/sandbox only; never write to customer production.

`.cursor/skills/demo-reset/` and its protection files stay in the repo. Use `make demo-reset`; do not use destructive Git reset to restore the seeded incident state.
