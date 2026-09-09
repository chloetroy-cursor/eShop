# Agent guidance

This remains the upstream .NET eShop reference application. Preserve application behavior and the existing `.cursor/skills/migration-*`, `migrate-to-rust`, and `migration-validate` assets.

Use pstack's `/poteto-mode` as the primary workflow for non-trivial work. Its playbook may call the incident and migration skills as specialist evidence tools. Proceed autonomously through reversible repo-local work: investigation, implementation, tests, local retry-policy edits, commits, pushes, and draft PRs.

For incident work, start at `docs/incidents/incident-001.md` and the ordering-api fixed telemetry window, not a random source file. Datadog and generated fallback telemetry are read-only evidence. Every incident claim needs the applicable request ID, absolute time window, monitor ID, release/version marker, and file citation. Once evidence identifies a local remediation, implement and verify it without a separate approval gate.

Keep pstack's irreversible-action boundary: production deploys, production data changes, force-pushes, merges without a merge/land/ship request, and external sends without a named authorized destination remain disallowed. Slack/Jira defaults to draft/sandbox; never write to customer production.

`chloetroy-cursor/eShop` is the only write target. The `upstream` remote, `shivamjindal/eShop`, is read-only: never push a branch, open a pull request, comment, or dispatch a workflow there. This repository is a fork, so `gh` resolves an unqualified command to the upstream parent and will target the wrong repository. Pass `--repo chloetroy-cursor/eShop` on every `gh` invocation that writes, and confirm the base repository in the returned URL before reporting a pull request as opened. A fork pull request also runs its workflows in the base repository, so a mistargeted pull request spends the parent's Actions quota and emails its maintainers.

`.cursor/skills/demo-reset/` and its protection files stay in the repo. Use `make demo-reset`; do not use destructive Git reset to restore the seeded incident state.
