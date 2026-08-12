---
name: Migrate slice to Rust
description: Use when implementing the first .NET→Rust migration slice for a service — characterize, extract if needed, implement the same rules in Rust, wire .NET to call Rust, prove parity, then run check-catalog.
---

# Migrate slice to Rust

Implement the first **.NET → Rust** migration slice for a service end-to-end: characterize current behavior, extract pure domain rules if still embedded, **port those rules to Rust**, **wire the .NET service to call Rust**, prove parity, and green `./scripts/check-catalog.sh`.

Stopping after a .NET-only extract is **not done**. Rust must be on the path (not dead code).

## Working style (explained here — no outside glossary)

1. **Tests before structural change** — Write characterization tests that lock *current* .NET behavior and get them green on the baseline *before* you extract or port.
2. **Small units that end green** — After each step, run the check and keep it green.
3. **Migrate callers, then delete legacy** — When extracting in .NET, move callers onto the extracted API in the same change, then delete the old duplicated / inlined path. No forever shim left behind.
4. **Rust is required for this slice** — Port the pure rules, wire .NET to call them, prove parity. Prefer a real library boundary over leaving Rust unused.
5. **Build a lever** — Prefer a committed script (`./scripts/check-catalog.sh`) that builds Rust (when present), runs Catalog tests, and fails closed.

## Default demo target

**CatalogItem stock rules** — `RemoveStock` / `AddStock` in `src/Catalog.API/Model/CatalogItem.cs` (or an extracted `CatalogStock` type) — unless the user points elsewhere.

**Rust crate path (canonical for this demo):** `native/catalog_stock/`  
(`cdylib` + `rlib` so .NET can P/Invoke and `cargo test` can exercise logic). If you must choose another path, document it in the PR and in `plan.md`.

## When to use

- After **Scope .NET → Rust** names a first harness-backed slice that ends in Rust + wire + parity
- Before claiming the slice is implemented for **Verify Catalog** / **Migration validate**
- When the previous “extract only” path would leave agents stopping too early

## Steps

1. **Brief `how` of current .NET behavior**  
   State current behavior in **3–5 bullets** (no long essay). Cover empty stock, qty ≤ 0, partial fill, max threshold, `OnReorder` as they apply to the target.

2. **Characterization tests on current .NET behavior**  
   Prefer adding `tests/Catalog.UnitTests/` if missing; otherwise extend an existing project. Cover at least: empty stock, qty ≤ 0, partial fill, max threshold, `OnReorder`. Tests must be runnable **without Docker** when possible.  
   Confirm they are **green on the baseline before** extract/port.

3. **Extract pure rules if still embedded**  
   Move pure domain rules to a clear module/type (same assembly is OK for the demo). Migrate callers, delete legacy duplicated logic in the same change. Keep characterization tests green.  
   Skip this step only if a clean pure surface already exists.

4. **Implement the same rules in Rust**  
   Create or extend a crate at `native/catalog_stock` (or the documented path):
   - `Cargo.toml` with `[lib] crate-type = ["cdylib", "rlib"]` (cdylib for .NET interop; rlib for `cargo test`)
   - Port the characterized stock rules with the **same semantics**
   - Unit tests in Rust that mirror the characterization cases  
   Run `cargo test` in that crate — must be green.

5. **Wire .NET to Rust for that island**  
   Pick the smallest honest boundary that works in-demo:
   - **Preferred:** P/Invoke / `LibraryImport` from the CatalogStock (or wrapper) type to the Rust `cdylib`
   - **Acceptable:** Rust CLI invoked for parity proof if FFI is too heavy for the environment — but prefer a real library call from the .NET wrapper  
   Catalog.API (or the domain wrapper it uses) must call Rust for the island. **Must not leave Rust as dead code never called.**

6. **Parity**  
   Same characterization cases must pass against the Rust path (and the .NET wrapper that delegates to Rust, or a dual-run harness). Record the command(s) and exit codes.

7. **Run the lever**  
   From repo root:

   ```bash
   ./scripts/check-catalog.sh
   ```

   Extend the script if needed so it builds/tests the Rust crate when present. Must be green (exit 0).  
   For stricter demos: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` fails if the expected Rust project is missing.

8. **Done / hand off**  
   Done **only when** Rust is on the path and checks are green.  
   Hand off to **Verify Catalog** + **Migration validate**; append decision-trail rows (`implement`, then validate as appropriate).

## Guardrails

- No unintended behavior change — characterization locks current semantics; Rust must match.
- No fake metrics — exit codes and failing/passing assertions only.
- Keep the diff **demo-small** — one pure rules island (stock), not a full service rewrite.
- Do not leave duplicated legacy + extracted logic side by side after the PR.
- Do not stop at .NET extract: **Rust implementation + wiring + parity are required.**
- Prefer `native/catalog_stock` so agents and `check-catalog.sh` share one convention.
