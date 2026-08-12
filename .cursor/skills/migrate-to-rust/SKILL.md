---
name: Migrate to Rust
description: Use when implementing the next unit(s) from a whole-service .NET→Rust plan — characterize, extract if needed, implement in Rust, wire .NET to call Rust, prove parity, then run check-catalog.
---

# Migrate to Rust

Implement the **next unit(s)** from a whole-service **.NET → Rust** plan (see **Scope .NET → Rust**): characterize current behavior, extract pure domain rules if still embedded, **port those rules to Rust**, **wire the .NET service to call Rust**, prove parity, and green the harness (`./scripts/check-catalog.sh` for Catalog).

Scoping plans the **entire service**; this skill executes **one (or a few) sequenced units/verticals** from that plan. Stopping after a .NET-only extract is **not done**. Rust must be on the path (not dead code).

## Working style (explained here — no outside glossary)

1. **Follow the plan** — Read `plan.md` (or the scoped sequence). Pick the next unfinished unit. Do not invent a tiny unrelated extract that ignores the rest of the service plan.
2. **Tests before structural change** — Write characterization tests that lock *current* .NET behavior and get them green on the baseline *before* you extract or port.
3. **Small units that end green** — After each step, run the check and keep it green.
4. **Migrate callers, then delete legacy** — When extracting in .NET, move callers onto the extracted API in the same change, then delete the old duplicated / inlined path. No forever shim left behind.
5. **Rust is required for the unit** — Port the pure rules, wire .NET to call them, prove parity. Prefer a real library boundary over leaving Rust unused.
6. **Build a lever** — Prefer a committed script (`./scripts/check-catalog.sh`) that builds Rust (when present), runs Catalog tests, and fails closed.

## Default demo target

**First Catalog.API unit — CatalogItem stock rules** — `RemoveStock` / `AddStock` in `src/Catalog.API/Model/CatalogItem.cs` (or an extracted `CatalogStock` type) — unless the plan / user points at another unit.

**Rust crate path (canonical for this demo):** `native/catalog_stock/`  
(`cdylib` + `rlib` so .NET can P/Invoke and `cargo test` can exercise logic). If you must choose another path, document it in the PR and in `plan.md`.

Later units from the same service plan use the same pattern (characterize → extract if needed → Rust → wire → parity) against their surface.

## When to use

- After **Scope .NET → Rust** produced a whole-service plan with sequenced units
- When implementing the next unfinished unit/vertical from that plan
- Before claiming the unit is ready for **Migration validate**

## Steps

1. **Select unit from the whole-service plan**  
   Open `plan.md`. Name the next unfinished unit and its acceptance checks. If no plan exists, run **Scope .NET → Rust** first (or draft the minimal plan covering the service) — do not proceed extract-only without a service-level sequence.

2. **Brief `how` of current .NET behavior**  
   State current behavior for this unit in **3–5 bullets** (no long essay). For stock: empty stock, qty ≤ 0, partial fill, max threshold, `OnReorder` as they apply.

3. **Characterization tests on current .NET behavior**  
   Prefer adding `tests/Catalog.UnitTests/` if missing; otherwise extend an existing project. Cover the unit’s cases. Tests must be runnable **without Docker** when possible.  
   Confirm they are **green on the baseline before** extract/port.

4. **Extract pure rules if still embedded**  
   Move pure domain rules to a clear module/type (same assembly is OK for the demo). Migrate callers, delete legacy duplicated logic in the same change. Keep characterization tests green.  
   Skip this step only if a clean pure surface already exists.

5. **Implement the same rules in Rust**  
   Create or extend a crate at `native/catalog_stock` (or the path from the plan):
   - `Cargo.toml` with `[lib] crate-type = ["cdylib", "rlib"]` (cdylib for .NET interop; rlib for `cargo test`)
   - Port the characterized rules with the **same semantics**
   - Unit tests in Rust that mirror the characterization cases  
   Run `cargo test` in that crate — must be green.

6. **Wire .NET to Rust for that island**  
   Pick the smallest honest boundary that works in-demo:
   - **Preferred:** P/Invoke / `LibraryImport` from the domain wrapper to the Rust `cdylib`
   - **Acceptable:** Rust CLI invoked for parity proof if FFI is too heavy for the environment — but prefer a real library call from the .NET wrapper  
   The service must call Rust for the island. **Must not leave Rust as dead code never called.**

7. **Parity**  
   Same characterization cases must pass against the Rust path (and the .NET wrapper that delegates to Rust, or a dual-run harness). Record the command(s) and exit codes.

8. **Run the lever**  
   From repo root (Catalog demo):

   ```bash
   ./scripts/check-catalog.sh
   ```

   Extend the script if needed so it builds/tests the Rust crate when present. Must be green (exit 0).  
   For stricter demos: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` fails if the expected Rust project is missing.  
   For non-Catalog units: run the unit’s harness from `plan.md` (`dotnet test`, `cargo test`, smoke).

9. **Done / hand off**  
   Done for this unit **only when** Rust is on the path and checks are green.  
   Hand off to **Migration validate**. Mark the unit complete in `plan.md` / tickets; leave remaining service units for later runs of this skill.

## Guardrails

- Execute against the **scoped whole-service plan** — verticals are fine; ignoring the rest of the service plan is not.  
- No unintended behavior change — characterization locks current semantics; Rust must match.  
- No fake metrics — exit codes and failing/passing assertions only.  
- Keep each unit **demo-small** — one island/vertical per run when possible, not a silent full-service rewrite.  
- Do not leave duplicated legacy + extracted logic side by side after the PR.  
- Do not stop at .NET extract: **Rust implementation + wiring + parity are required.**  
- Prefer `native/catalog_stock` for the Catalog stock unit so agents and `check-catalog.sh` share one convention.  
- Do not depend on removed skills (characterize-then-extract, verify-catalog, migration-decision-trail).
