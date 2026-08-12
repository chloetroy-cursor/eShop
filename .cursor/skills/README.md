# Cursor skills — Intellias migration demo pack

Cold-agent playbooks for a **.NET → Rust** brownfield migration of one service. No external framework knowledge required.

**Order of use**

1. **Scope .NET → Rust** — Point at one .NET service. Inventory the **whole service** (public surface, domain, adapters, events, deps), map blast radius for migrating that service toward Rust, and emit a **whole-service migration plan** sequenced into verifiable units (first unit may be the first vertical to implement). Rust is the required end state for the planned units → `plan.md`.
2. **Migrate to Rust** — Implement the next unit(s) from that whole-service plan: characterize → extract if needed → Rust → wire → parity → `./scripts/check-catalog.sh` (for Catalog). Can land verticals; the plan covers the full service.
3. **Migration validate** — Rank evidence honestly (self-report < pointing at code < actually ran tests). For migrated units, keep/merge requires Rust on the path + parity evidence. Use `./scripts/check-catalog.sh` and cargo/parity output directly. Decide **keep/merge**, **do not merge**, or **inconclusive** → `validate.md`.

Lever (not a skill): `./scripts/check-catalog.sh` — builds/tests Rust when present, then Catalog unit/functional tests.
