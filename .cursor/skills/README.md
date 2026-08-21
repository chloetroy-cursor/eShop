# Cursor skills

Three packs share this repository. The first two are native to this demo.
The third is a verbatim vendor of [pstack](https://github.com/cursor/plugins/tree/main/pstack)
(see `.cursor/plugins/SOURCE.md` and `docs/pstack/README.md`).

## .NET → Rust migration

Advanced entrypoint: `migration-program`

`migration-program` composes three skills through planner, implementer, and
independent validator subagents with evidence gates:

1. `migration-scope` — inventory one whole .NET service and propose a sequenced plan.
2. `migrate-to-rust` — characterize, port the next planned unit, wire Rust onto the live path, and prove parity.
3. `migration-validate` — independently rank evidence and decide keep/merge, do not merge, or inconclusive.

These playbooks are service-agnostic. Catalog.API and `native/catalog_stock` are examples, not defaults; use the harness named in the service plan.

## Incident response

Entrypoint: `incident-response`

1. `query-service-health` — retrieve ordering-api health, monitor, and deploy evidence for the fixed window.
2. `trace-request` — follow one request across read-only logs and traces.
3. `correlate-release` — align incident onset with release and Git evidence.
4. `incident-response` — launch telemetry, change, code-path, and challenger specialists in parallel; synthesize, remediate locally, and verify.
5. `write-incident-update` — turn cited evidence into a draft-only Slack/Jira update.

Use `demo-reset` or `make demo-reset` to restore the seeded INC-001 start. Start at `docs/incidents/incident-001.md`; retry-policy remediation may proceed autonomously after evidence. Production deploys, merges, and external sends still require a matching operator request.

## pstack (vendored)

Entrypoint: `poteto-mode`

Workspace skills and agents are symlinks into `.cursor/plugins/pstack/`. Use `/poteto-mode` as the primary workflow; incident and migration skills are specialist tools it may invoke. Reversible repo-local work runs autonomously. Per-role models live in `.cursor/rules/pstack-models.mdc`.

## Field Engineer demo sessions

Entrypoint: `reset-fe-demo`

`reset-fe-demo` replaces a clean dedicated demo worktree with a fresh branch
from `origin/main`, runs the protected INC-001 reset, and returns the path for a
new Cursor window and Agent chat. It never rewrites history or discards dirty
work.

Then run `demo-prep`. It checks .NET 9, Docker, and tmux; starts the .NET Aspire
AppHost; waits for the Blazor storefront at `http://localhost:5045`; and returns
the storefront, Aspire dashboard, and log locations.
