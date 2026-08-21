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

## The demo path

Four prompts, front to back. Type them as-is.

**0. Reset — fresh repo and chat**

```text
/reset-fe-demo
```

Open the returned path in a new Cursor window and start a new Agent chat.

**0.5. Prep — open the storefront**

```text
/demo-prep
```

This starts the Aspire stack and opens the Blazor WebApp at
`http://localhost:5045`.

Run this on the machine you are presenting from. `localhost` belongs to
whichever machine runs AppHost, so a storefront started inside a cloud agent
lives in that VM and is only reachable through Cursor's port forwarding while
that agent is connected and running.

**1. Canvas — understand the repo**

```text
/canvas architecture of this repo, with a diagram
```

**2. Plan mode — pick the feature**

```text
the webapp never shows stock. plan it.
```

Catalog.API already returns `availableStock`; the WebApp drops it. Plan mode finds
that, which is a better story than inventing a new field.

**3. Cloud agent — build it while you talk**

```text
/poteto-mode show stock in the webapp. verify it, open a pr.
```

**4. GitHub — PR and Bugbot**

Open the PR, show the agent's evidence, let Bugbot review. If it finds something
real:

```text
/poteto-mode babysit this pr
```

Then land it:

```text
/poteto-mode land it
```

### Preflight

```bash
dotnet build eShop.Web.slnf
```

Run preflight from the fresh demo path. `/demo-prep` requires .NET 10 and a
running Docker (Docker Desktop is fine locally). The build alone still proves
the change if the visual stack is not available.

### Other one-liners

```text
/how does ordering publish to rabbitmq?
```

```text
/why is stock checked in Catalog.API?
```

```text
/interrogate this branch
```

```text
/poteto-mode fix INC-001. start at docs/incidents/incident-001.md.
```

## Shareable setup for another machine

1. Clone this repo and open the folder in Cursor.
2. Confirm workspace skills include `poteto-mode`, `how`, `why`, `architect`, `arena`, `interrogate`.
3. If a skill is missing, Reload Window. Skills are git-tracked symlinks; `git clone` must preserve them (default).
4. If your model list differs, run `/setup-pstack` and accept or remap roles. Prefer writing the project rule `.cursor/rules/pstack-models.mdc` so the mapping stays with the demo.

A project-local `/create-verification-skill` was **not** generated. eShop already has service tests, Playwright workflows, and migration/incident harnesses. Generate a `verify-*` skill later if a live UI drive is part of the demo.

## Refreshing the vendor copy

Do not edit `.cursor/plugins/pstack/`. Replace the directory from upstream, keep `SOURCE.md` updated with the new commit, and re-check that skill/agent symlinks still resolve.
