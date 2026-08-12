---
name: Migrate to Rust
description: Use when implementing the next unit(s) from a whole-service .NET→Rust plan for any .NET service — characterize, extract if needed, implement in Rust, wire the .NET host to call Rust, prove parity, run the unit harness from plan.md.
---

# Migrate to Rust

Implement the **next unit(s)** from a whole-service **.NET → Rust** plan (see **Scope .NET → Rust**).

For the chosen unit: characterize current .NET behavior → extract pure rules if still embedded → **port to Rust** → **wire the .NET service to call Rust** → prove parity → green the harness named in `plan.md`.

Scoping plans the **entire service**; this skill executes **one (or a few) sequenced units** from that plan. Stopping after a .NET-only extract is **not done**. Rust must be on the live path (not dead code).

Works for **any** .NET service the plan names (API, worker, gRPC host, etc.). Repo-specific paths and scripts come from `plan.md` / the ticket — do not assume Catalog.API.

## Inputs

| Item | Source |
|------|--------|
| `SERVICE` | From `plan.md` / ticket / user (e.g. Catalog.API, Basket.API, Ordering.API) |
| Next unit | Next unfinished unit in `plan.md` **Recommended sequence** |
| Harness | Command listed for that unit in `plan.md` (script, `dotnet test …`, `cargo test`, smoke) |
| Rust crate path | From `plan.md`, or `native/crates/<service>/src/<unit>.rs` |

If there is no plan, run **Scope .NET → Rust** first (or draft the minimal whole-service sequence). Do not proceed extract-only without a service-level plan.

## Working style

1. **Follow the plan** — Read `plan.md`. Pick the next unfinished unit. Do not invent an unrelated extract that ignores the rest of the service plan.
2. **Tests before structural change** — Characterization tests lock *current* .NET behavior and must be green on the baseline *before* extract or port.
3. **Small units that end green** — After each step, run the unit's check command; keep it green.
4. **Migrate callers, then delete legacy** — When extracting in .NET, move callers onto the extracted API in the same change, then delete the old inlined path. No forever shim.
5. **Rust is required for the unit** — Port the pure rules, wire .NET to call them, prove parity. Prefer a real library boundary over leaving Rust unused.
6. **Use the plan's lever** — Prefer a committed harness/script from `plan.md` that fails closed (builds Rust when present, runs the right tests). If missing, add a small script or document the exact `dotnet test` / `cargo test` commands in the PR and `plan.md`.

## When to use

- After **Scope .NET → Rust** produced a whole-service plan with sequenced units
- When implementing the next unfinished unit/vertical from that plan
- Before claiming the unit is ready for **Migration validate**

## Steps

1. **Select unit from the whole-service plan**  
   Open `plan.md`. Name `SERVICE`, the next unfinished unit, its acceptance checks, and its harness command. If the plan is missing or only covers a tiny slice of the service, stop and re-scope.

2. **Brief `how` of current .NET behavior**  
   State current behavior for **this unit** in **3–5 bullets** (no long essay). Cover edge cases the characterization suite will lock.

3. **Characterization tests on current .NET behavior**  
   Add or extend tests under the service's existing test project(s) (create a focused unit-test project only if none exist). Cover the unit's cases. Prefer runnable **without Docker** when possible.  
   Confirm they are **green on the baseline before** extract/port.

4. **Extract pure rules if still embedded**  
   Move pure domain rules to a clear module/type (same assembly is OK for a demo). Migrate callers, delete legacy duplicated logic in the same change. Keep characterization tests green.  
   Skip only if a clean pure surface already exists.

5. **Implement the same rules in Rust**  
   Create or extend the unit module at the path from `plan.md` (convention: `native/crates/<service>/src/<unit>.rs`, e.g. `catalog::stock`):
   - Service crate `Cargo.toml` with `[lib] crate-type = ["cdylib", "rlib"]` when .NET will P/Invoke
   - Port the characterized rules with the **same semantics**
   - Unit tests in Rust that mirror the characterization cases  
   Run `cargo test -p <service>` from `native/` — must be green. Record the path in `plan.md` if new.

6. **Wire .NET `SERVICE` to Rust for that island**  
   Smallest honest boundary that works in-demo:
   - **Preferred:** P/Invoke / `LibraryImport` from the domain wrapper to the Rust `cdylib`
   - **Acceptable:** Rust CLI invoked for parity proof if FFI is too heavy — but prefer a real library call from the .NET wrapper  
   The service must call Rust for the island. **Must not leave Rust as dead code never called.**

7. **Parity**  
   Same characterization cases must pass against the Rust-wired path (wrapper that delegates to Rust, or a dual-run harness). Record command(s) and exit codes.

8. **Run the harness from `plan.md`**  
   Run the exact check command listed for this unit (or the service lever if the plan names one). Extend that script if needed so it builds/tests the Rust crate when present. Must be green (exit 0).  
   If the plan supports a "require Rust" mode for demos, use it when proving cutover.  
   Do **not** default to a Catalog-only script unless `SERVICE` is Catalog and that script is what `plan.md` names.

9. **Done / hand off**  
   Done for this unit **only when** Rust is on the path and checks are green.  
   Hand off to **Migration validate**. Mark the unit complete in `plan.md` / tickets; leave remaining service units for later runs of this skill.

## eShop examples (not defaults)

Use only when the plan/ticket points here — patterns to copy, not assumptions for every service:

| Service | Example first unit | Example crate | Example harness |
|---------|--------------------|---------------|-----------------|
| Catalog.API | CatalogItem stock (`RemoveStock` / `AddStock`) | `native/crates/catalog` (`catalog::stock`) | `./scripts/check-catalog.sh` |
| Basket.API | Basket line total / quantity rules (as scoped) | `native/crates/basket` (`basket::cart`) | `dotnet test` + `cargo test` (or a `scripts/check-basket.sh` if added) |
| Ordering | One command/handler vertical (as scoped) | `native/crates/ordering` (`ordering::orders`) | harness named in that service's `plan.md` |

## Guardrails

- Execute against the **scoped whole-service plan** — verticals are fine; ignoring the rest of the service plan is not.
- **Service-agnostic** — Catalog.API / `check-catalog.sh` / `native/crates/catalog` are eShop examples only.
- No unintended behavior change — characterization locks current semantics; Rust must match.
- No fake metrics — exit codes and failing/passing assertions only.
- Keep each unit **demo-small** — one island/vertical per run when possible.
- Do not leave duplicated legacy + extracted logic side by side after the PR.
- Do not stop at .NET extract: **Rust implementation + wiring + parity are required.**
- Do not depend on removed skills (characterize-then-extract, verify-catalog, migration-decision-trail).
