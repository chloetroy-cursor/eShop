# Cursor skills — Intellias migration demo pack

Cold-agent playbooks for a **.NET → Rust** brownfield slice. No external framework knowledge required.

**Order of use**

1. **Scope .NET → Rust** — Point at one .NET service. Inventory it, map who depends on it, write a checkable definition of done, and pick a first small slice with a test/script that can fail before you restructure code → `plan.md`.
2. **Characterize then extract** — Lock current behavior with tests first, extract pure domain rules, move callers onto the new API and delete the old duplicated path in the same change, then run `./scripts/check-catalog.sh`.
3. **`./scripts/check-catalog.sh` / Verify Catalog** — Prove Catalog.API still behaves the way a client would care about; capture real exit codes (not vibes).
4. **Migration validate** — Rank evidence honestly (self-report < pointing at code < actually ran tests). Prove the one safety fact by running code. Decide **keep/merge this slice**, **do not merge**, or **inconclusive** (could not get evidence — that is *not* keep/merge). Cap fix attempts at 3; encode recurring failures as tests/scripts/lints, not longer prompts → `validate.md`.
5. **Migration decision trail** — Append-only `migrations/decisions.tsv` at scope / implement / validate so a reviewer can see what was decided and why.
