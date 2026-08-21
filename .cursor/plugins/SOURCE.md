# Vendored Cursor plugins

## pstack

- **Upstream:** https://github.com/cursor/plugins/tree/main/pstack
- **Pinned commit:** `46125561306434d8a1d7745d540d8932ab0cd2a2`
- **Copied on:** 2026-08-21
- **Local path:** `.cursor/plugins/pstack/`

This directory is a **verbatim copy**. Do not edit files under `.cursor/plugins/pstack/`. Refresh by replacing the tree from the pinned (or a newer) upstream commit, then keep the workspace skill/agent symlinks in `.cursor/skills/` and `.cursor/agents/`.

Repo-local wiring (not part of upstream):

- `.cursor/skills/<skill>` → `.cursor/plugins/pstack/skills/<skill>`
- `.cursor/agents/poteto-agent.md` and `comment-sicko.md` → `.cursor/plugins/pstack/agents/`
- `.cursor/rules/pstack-models.mdc` — per-role models confirmed for this environment
- `.cursor/rules/pstack-eshop.mdc` — eShop demo gates vs pstack shipping playbooks
- `docs/pstack/README.md` — how this demo uses pstack

Benny automations stay inside the vendor copy and are **not** registered under `.cursor/automations/`.
