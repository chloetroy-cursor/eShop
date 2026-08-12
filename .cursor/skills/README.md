# Cursor skills — Intellias migration demo pack

Cold-agent playbooks for a **.NET → Rust** brownfield slice. No external framework knowledge required.

**Order of use**

1. **Scope .NET → Rust** — Point at one .NET service. Inventory it, map who depends on it, write a checkable definition of done, and pick a first small slice that ends in **Rust + wire + parity** (not extract-only) → `plan.md`.
2. **Migrate slice to Rust** — Characterize current .NET behavior, extract pure rules if needed, **implement the same rules in Rust** (`native/catalog_stock`), **wire Catalog.API to call Rust**, prove parity, then run `./scripts/check-catalog.sh`. (Replaces the old “Characterize then extract” skill, which stopped too early.)
3. **`./scripts/check-catalog.sh` / Verify Catalog** — Build/test Rust when present; prove Catalog.API still behaves the way a client would care about (through the Rust-wired path when present); capture real exit codes (not vibes).
4. **Migration validate** — Rank evidence honestly (self-report < pointing at code < actually ran tests). For Catalog inventory/stock: keep/merge requires Rust implementation + parity evidence, not .NET extract alone. Prove the one safety fact by running code. Decide **keep/merge this slice**, **do not merge**, or **inconclusive** (could not get evidence — that is *not* keep/merge). Cap fix attempts at 3; encode recurring failures as tests/scripts/lints, not longer prompts → `validate.md`.
5. **Migration decision trail** — Append-only `migrations/decisions.tsv` at scope / implement / validate so a reviewer can see what was decided and why.

Deprecated stub: `characterize-then-extract/` points at **Migrate slice to Rust** — do not use it for new work.
