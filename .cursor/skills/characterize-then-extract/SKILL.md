---
name: Characterize then extract
description: Deprecated stub — use Migrate slice to Rust instead. Kept so old links resolve.
---

# Characterize then extract (deprecated)

This skill has been **replaced** by **Migrate slice to Rust**.

Agents were stopping after a .NET-only extract (`CatalogStock` + unit tests). The demo now requires an end-to-end slice: characterize → extract (if needed) → **Rust port** → **wire Catalog.API to Rust** → **parity** → `./scripts/check-catalog.sh`.

**Use instead:** [Migrate slice to Rust](../migrate-slice-to-rust/SKILL.md) (`.cursor/skills/migrate-slice-to-rust/`).

Do not implement new work from this stub.
