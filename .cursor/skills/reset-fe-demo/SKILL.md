---
name: reset-fe-demo
description: Creates a fresh, isolated eShop demo worktree from origin/main without deleting prior demo work or using git reset/clean. Always pass --profile incident or visual.
---

# Reset the Field Engineer demo

Always pass `--profile` explicitly. Infer it from the active flow. Do not ask when the context identifies it.

- Choose `incident` for INC-001, ordering-api telemetry, RabbitMQ retry, or incident-response flows.
- Choose `visual` for storefront, catalog, product, stock, UI, browser, and visual-demo flows.

Both profiles start from latest `origin/main`, so merged UI remains. A profile only controls scenario-specific overlays.

Run once, with unrestricted permissions, from the original clone:

```bash
python3 .cursor/skills/reset-fe-demo/scripts/reset.py --profile incident
```

The script writes `~/.cursor/demo-worktrees/<repo>` and fetches `origin`. Request
those permissions on the first Shell call. Do not run it sandboxed first, hunt
for another Python, or edit the script. It is Python 3.9 compatible.

A clean existing demo worktree is reused in place: new branch from `origin/main`,
same path, previous branch and PR left intact. The incident profile then runs
`make demo-reset` inside it. Never use `git reset` or `git clean`.

Return the generated path and branch. Tell the operator to open that path in a
new Cursor window and start a new Agent chat. Repository state can be reset;
conversation context cannot.

If the previous demo worktree is dirty, stop and list its changed paths. Do not
discard, stash, commit, or copy them automatically.
