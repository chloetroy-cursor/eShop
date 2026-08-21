# pstack in this eShop demo

This repo vendors [pstack](https://github.com/cursor/plugins/tree/main/pstack) so Field Engineer demos can run `/poteto-mode` without a separate marketplace install. Upstream files are copied as-is under `.cursor/plugins/pstack/` (see `.cursor/plugins/SOURCE.md`). Original pstack docs inside that tree are not edited.

## What is wired

| Piece | Where it lives |
| --- | --- |
| Verbatim plugin | `.cursor/plugins/pstack/` |
| Slash skills | `.cursor/skills/<name>` → plugin skills (symlinks) |
| `poteto-agent`, Comment Sicko | `.cursor/agents/` → plugin agents (symlinks) |
| Per-role models | `.cursor/rules/pstack-models.mdc` |
| eShop autonomous policy | `.cursor/rules/pstack-eshop.mdc` |

Clone the repo, open it in Cursor, and `/poteto-mode` should resolve as a workspace skill. Optional: also `/add-plugin pstack` from the marketplace if you want marketplace updates; this copy is pinned.

Re-run `/setup-pstack` if your entitled model slugs differ from the ones in `pstack-models.mdc`. This environment mapped pstack's default panel onto slugs that `Task` actually accepted here.

Benny automations shipped with pstack stay dormant inside the vendor tree. They are not copied into `.cursor/automations/` so they do not collide with INC-001 Slack/Datadog automations.

## How this drives the existing demos

pstack is the primary router. It may use the two original packs as specialist
evidence and verification tools:

- **Incident:** `docs/incidents/incident-001.md` + `incident-response`
- **Migration:** `migration-program` / `migrate-to-rust` / `migration-validate`

Reversible repo-local work proceeds without a separate approval handoff,
including incident remediation, retry-policy edits, migration units, commits,
pushes, and draft PRs. Production deploys, merges, and external sends retain
pstack's own irreversible-action boundary.

## First demo prompts (eShop)

Type these as-is after the repo is open. They are starters for a later prompt-library pass.

**Understand (read-only)**

```text
/how does ordering-api publish integration events to RabbitMQ, and where is retry configured?
```

```text
/why is catalog stock checked in Catalog.API instead of in the basket service?
```

```text
/teach me how a checkout becomes an order. diagram by diagram. do not edit anything.
```

**Design before code**

```text
/architect a small Catalog.API change that returns remaining stock on the item DTO. settle types and call sites first. do not implement yet.
```

```text
/arena propose three shapes for that remaining-stock field. I want to compare them before we write code.
```

**Build with evidence**

```text
/poteto-mode add remaining stock to the catalog item API response. repro the current payload first, then implement the smallest change, and prove it with a real response or existing test run.
```

```text
/tdd cover the remaining-stock field if there is a cheap existing test host. if the test path is mock-heavy, say so and use the real API instead.
```

**Review**

```text
/interrogate this branch. read-only. no nitpicks unless it is a behavior regression.
```

**Autonomous incident remediation**

```text
/poteto-mode start at docs/incidents/incident-001.md. Use incident-response for cited, read-only telemetry; reproduce the retry failure, implement the smallest root-cause fix, run the incident harness, interrogate the diff, commit, push, and update the draft PR. Do not stop for permission during reversible local work.
```

## Shareable setup for another machine

1. Clone this repo and open the folder in Cursor.
2. Confirm workspace skills include `poteto-mode`, `how`, `why`, `architect`, `arena`, `interrogate`.
3. If a skill is missing, Reload Window. Skills are git-tracked symlinks; `git clone` must preserve them (default).
4. If your model list differs, run `/setup-pstack` and accept or remap roles. Prefer writing the project rule `.cursor/rules/pstack-models.mdc` so the mapping stays with the demo.

A project-local `/create-verification-skill` was **not** generated. eShop already has service tests, Playwright workflows, and migration/incident harnesses. Generate a `verify-*` skill later if a live UI drive is part of the demo.

## Refreshing the vendor copy

Do not edit `.cursor/plugins/pstack/`. Replace the directory from upstream, keep `SOURCE.md` updated with the new commit, and re-check that skill/agent symlinks still resolve.
