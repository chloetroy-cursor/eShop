---
name: demo-prep
description: Starts the eShop Aspire stack, waits for the Blazor storefront, and opens the visual demo surface.
---

# Prepare the visual demo

Run:

```bash
python3 .cursor/skills/demo-prep/scripts/prep.py start
```

The script checks .NET 10, Docker, and tmux; starts `eShop.AppHost` in the
`eshop-fe-demo` tmux session; and waits for `http://localhost:5045`.

When ready, open `http://localhost:5045` with the available browser/computer
tool and confirm the catalog renders. Return the storefront URL, Aspire
dashboard URL, and log path. If no browser tool is available, return clickable
links.

Use `status` to inspect an existing stack and `stop` to stop the tmux session:

```bash
python3 .cursor/skills/demo-prep/scripts/prep.py status
python3 .cursor/skills/demo-prep/scripts/prep.py stop
```

Do not start a second AppHost when the session or storefront is already live.
