---
name: demo-prep
description: Starts the eShop Aspire stack, waits for the Blazor storefront, and opens the visual demo surface.
---

# Prepare the visual demo

Run:

```bash
python3 .cursor/skills/demo-prep/scripts/prep.py start
```

The script checks .NET 10 and Docker, starts `eShop.AppHost`, and waits for
`http://localhost:5045`. It uses the `eshop-fe-demo` tmux session when tmux is
available and a detached background process otherwise, so it works on a plain
laptop.

When ready, open `http://localhost:5045` with the available browser/computer
tool and confirm the catalog renders. Return the storefront URL, Aspire
dashboard URL, and log path. If no browser tool is available, return clickable
links.

`localhost` is the machine running AppHost. Run this skill on the presenter's
own machine for a live demo. In a cloud agent the storefront exists only inside
that VM, reachable through Cursor's port forwarding while the agent is
connected and running.

Use `status` to inspect an existing stack and `stop` to stop the tmux session:

```bash
python3 .cursor/skills/demo-prep/scripts/prep.py status
python3 .cursor/skills/demo-prep/scripts/prep.py stop
```

Do not start a second AppHost when the session or storefront is already live.
