---
name: reset-fe-demo
description: Creates a fresh, isolated eShop demo worktree from origin/main without deleting prior demo work or using git reset/clean.
---

# Reset the Field Engineer demo

Run:

```bash
python3 .cursor/skills/reset-fe-demo/scripts/reset.py
```

The script replaces the dedicated worktree under
`~/.cursor/demo-worktrees/<repo>` with a new branch from `origin/main`, then
runs `make demo-reset` inside it. It removes the previous demo worktree only
when it is clean; its branch and PR remain intact. Never use `git reset` or
`git clean`.

Return the generated path and branch. Tell the operator to open that path in a
new Cursor window and start a new Agent chat. Repository state can be reset;
conversation context cannot.

If the previous demo worktree is dirty, stop and list its changed paths. Do not
discard, stash, commit, or copy them automatically.
