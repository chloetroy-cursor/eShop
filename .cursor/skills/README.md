# Cursor skills

Three packs share this repository. The first two are native to this demo.
The third is a verbatim vendor of [pstack](https://github.com/cursor/plugins/tree/main/pstack)
(see `.cursor/plugins/SOURCE.md` and `docs/pstack/README.md`).

## .NET → Rust migration

Advanced entrypoint: `migration-program`

`migration-program` composes three skills through planner, implementer, and
independent validator subagents with human gates:

1. `migration-scope` — inventory one whole .NET service and propose a sequenced plan.
2. `migrate-to-rust` — characterize, port the next approved unit, wire Rust onto the live path, and prove parity.
3. `migration-validate` — independently rank evidence and decide keep/merge, do not merge, or inconclusive.

These playbooks are service-agnostic. Catalog.API and `native/catalog_stock` are examples, not defaults; use the harness named in the service plan.

## Incident response

Entrypoint: `incident-response`

1. `query-service-health` — retrieve ordering-api health, monitor, and deploy evidence for the fixed window.
2. `trace-request` — follow one request across read-only logs and traces.
3. `correlate-release` — align incident onset with release and Git evidence.
4. `incident-response` — launch telemetry, change, code-path, and challenger specialists in parallel; synthesize and stop at the human gate.
5. `write-incident-update` — turn cited evidence into a draft-only Slack/Jira update.

Use `demo-reset` or `make demo-reset` to restore the seeded INC-001 start. Start at `docs/incidents/incident-001.md`; do not edit retry behavior, deploy, merge, or send externally without explicit approval.

## pstack (vendored)

Entrypoint: `poteto-mode`

Workspace skills and agents are symlinks into `.cursor/plugins/pstack/`. Use `/poteto-mode` for rigorous coding work. Do not use pstack shipping/overnight playbooks to bypass incident or migration approval gates. Per-role models live in `.cursor/rules/pstack-models.mdc`.
