# Cursor skills — Intellias migration demo pack

Order of use: **Scope .NET → Rust** (point at a service → `plan.md`) → **Characterize then extract** (tests first, extract pure rules, migrate callers then delete legacy) → **`./scripts/check-catalog.sh` / Verify Catalog** (lever + client-style evidence for Catalog.API) → **Migration validate** (Go / No-go / Inconclusive + blast-radius proof). Keep an append-only trail via **Migration decision trail** (`migrations/decisions.tsv`).
