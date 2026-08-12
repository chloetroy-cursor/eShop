---
name: Scope .NET → Rust
description: Use when scoping a .NET → Rust migration for one entire service the user points at. Inventory the whole service, map blast radius, and produce a whole-service migration plan sequenced into verifiable units (Rust required end state).
---

# Scope .NET → Rust

Playbook for scoping a **.NET → Rust** brownfield migration of **one entire service**. Tickets already describe the work — the user points at a service; you scope **all of it**. Do not ask them to fill a source/target parameter table.

**Fixed direction:** always migrate **from .NET toward Rust**. The only input is which service. Rust is the **required** end state for the planned units — not optional, not a later spike.

## Input

The **service** (or project) the user points at — folder, project name, ticket link, or @ mention.

If unclear, infer once from the open ticket / chat / workspace context (e.g. Catalog.API in eShop). Do **not** interrogate for stack parameters, entrypoints, or repo metadata. If still ambiguous after one inference pass, ask only: *which .NET service?*

Optional context (take if offered, don’t block on it): constraints (latency, dual-run, regulatory), ticket system, Aspire/hosting notes.

## When to use

- User (or ticket) names a .NET service to migrate toward Rust
- Need a **whole-service** inventory → blast radius → sequenced units before production edits
- Demo or engagement where the stack is already .NET → Rust

## Ideas this skill expects you to follow (explained here — no outside glossary)

- **Whole-service plan** — Definition of done for this scoping run is a plan that covers the **entire** service with sequenced, verifiable units — not a tiny “one CatalogItem extract” mindset. The first unit can still be the first vertical to implement, but the artifact must plan the full service.
- **Checkable definition of done** — Concrete pass/fail bullets for *this run*. Someone reopening the artifact must be able to say pass or fail. No vibes. Planned units that stop after a .NET extract are incomplete; Rust + wire + parity are required for each domain island you schedule.
- **Safety fact** — Name the *one* fact the first unit depends on. Mark it **proven** only if you ran code/tests/commands; otherwise **unproven**. Design docs and narration do not count.
- **Small units that each end green** — Sequence the service migration as change → run a check command → green, then the next unit. Never one big-bang rewrite as the first move. Units can be verticals (API + domain + adapter for one capability) as long as together they cover the service.
- **Harness / tests before structural change** — Prefer a characterization test suite or script that fails closed on drift *before* you restructure, extract, or port.

## Steps

1. **Resolve service + checkable definition of done (whole service)**  
   Name the service (`SERVICE`). Success = a **whole-service migration plan** toward Rust, sequenced into safe units — not “rewrite the estate in one PR” and not “stop after one tiny extract.”  
   Write a **definition of done** for *this scoping run* — checkable, e.g.:
   - `plan.md` inventories the full public surface, domain, adapters, events, and deps of `SERVICE`
   - Blast radius covers migrating **the service** (inbound/outbound, local vs cross-cutting)
   - Recommended sequence is a set of verifiable units that **together cover the service**, each ending in a green check, with Rust + wire + parity required for each domain island scheduled
   - First unit names a harness command that can fail before mass edits (`./scripts/check-catalog.sh` for Catalog)  
   Someone must be able to reopen the artifact and say pass/fail. No vibes.

2. **Inventory the whole `SERVICE` (.NET-aware)**  
   Cover the **entire** service, not a single method:
   - Assemblies / projects under the service: API, Domain, Infrastructure, tests (`.csproj`, solution filters).  
   - TFM(s), SDK-style vs legacy, NuGet / Central Package Management, build entrypoints.  
   - Hosting & boundaries: Aspire AppHost, Kestrel endpoints, gRPC, workers, message consumers.  
   - **Public surface:** all HTTP/gRPC routes, published events/commands, CLI/batch entrypoints.  
   - **Domain core** vs **adapters** (EF/DB, brokers, auth, third-party, file/blob).  
   - **Events** produced/consumed; contracts and shared types.  
   - **Dependencies:** NuGet, project refs, external services, shared kernels.  
   - Note any existing `native/` Rust crates (e.g. `native/catalog_stock`) if present.  
   Write under **Inventory** in `plan.md`.

3. **Map dependencies & blast radius for migrating the service**  
   - Inbound: who calls `SERVICE` (other services, UI, jobs, Aspire references).  
   - Outbound: DB schemas, brokers, shared .NET libs/contracts.  
   - Shared types that would force multi-service changes.  
   - Test hotspots and untested critical paths (`dotnet test` projects).  
   Label each edge: *local* vs *cross-cutting*.  
   Think blast radius for migrating **toward Rust for this service** (can still sequence into multiple units).  
   **Safety fact:** the *one* fact the first unit depends on (e.g. “Catalog stock rules are I/O-free”, “endpoint Y has characterization coverage”, “schema Z owned only by SERVICE”).  
   Mark **proven** (you ran code/tests/commands) or **unproven** (assumption only). Design docs don’t count. If unproven, the first unit must prove it or the unit is blocked.  
   Write under **Dependencies / blast radius**.

4. **Propose .NET → Rust sequence as verifiable units covering the service**  
   Prefer verticals that compile, test, and ship independently. Each unit: *change → check (command) → green* before the next.  
   Units together must cover the inventoried service surface (public APIs, domain islands, adapters, events) — not leave most of the service unplanned.  
   **Typical progression inside each domain island / vertical** (adapt names; do not drop the Rust steps):
   1. Characterize current .NET behavior (`dotnet test` / characterization suite).  
   2. Extract pure .NET domain rules if still embedded (migrate callers, delete legacy) — keep tests green.  
   3. **Rust port of those rules** (crate under `native/<service>_<unit>/` or equivalent) — `cargo test` green.  
   4. **Wire `SERVICE` to call Rust** for that island (preferred: P/Invoke / `LibraryImport` to a `cdylib`; acceptable for parity proof: Rust CLI if FFI is too heavy — but prefer a real library call from the .NET wrapper). Rust must not remain dead code.  
   5. **Parity harness** — same characterization cases pass against the Rust path (use harness from plan).  
   Avoid big-bang .NET→Rust rewrite of all of `SERVICE` as the first move — but **do not** treat Rust as optional or “later spike,” and **do not** stop the plan at a single tiny extract.  
   Write under **Recommended sequence** with enough units to cover the service.

5. **Call out risks**  
   For each major risk: trigger, impact, mitigation, detection (test/metric/alert).  
   Themes to adapt (no fake numbers): behavioral drift, shared-kernel / NuGet coupling, serialization & TFM quirks, missing characterization tests, Rust ops/runtime skill gap, FFI/ABI or process-boundary deploy complexity, parity gaps between .NET and Rust, unfinished service surface left unplanned.  
   Write under **Risks**.

6. **Name the first unit (first vertical) — still within the whole-service plan**  
   Smallest first unit that:
   - is mostly pure or well-bounded in .NET,
   - can gain characterization tests *before* behavior changes,
   - proves characterize/extract → **Rust port → wire → parity** → validate,
   - does **not** pretend the whole service is done after this one unit — later units remain in the plan,  
   - is **not** “extract only” — extract (if needed) is a stepping stone to the Rust island.  
   **Harness before change:** write or identify a characterization harness / script that fails closed on drift, *then* do structural edit, Rust port, and wiring. Command from plan (service-specific script, `dotnet test` + `cargo test`, or smoke).  
   Example (Catalog.API): first unit = characterize `RemoveStock` / `AddStock` → extract if needed → `native/catalog_stock` → wire → parity + `./scripts/check-catalog.sh`; later units cover remaining Catalog.API surface (queries, other domain rules, adapters, events) per the inventory.  
   Write under **First unit** with acceptance checks, harness command, suggested ticket titles. Keep the full sequence in **Recommended sequence**.

7. **Emit artifact**  
   Write `plan.md` at the workspace/migration root (or path the user specifies). Actionable for agent or human. No fake metrics. Re-check the definition of done before calling the run complete — whole-service coverage + required Rust + wire + parity in the planned units.

## Output: `plan.md` template

```markdown
# .NET → Rust migration scope: {SERVICE}

## Definition of done
- [ ] Whole-service inventory (public surface, domain, adapters, events, deps) complete
- [ ] Blast radius for migrating the service documented (local vs cross-cutting)
- [ ] Recommended sequence covers the service with sequenced verifiable units
- [ ] Each scheduled domain island includes required Rust implementation + .NET→Rust wire + parity
- How to check pass/fail: ...

## Inventory
- Assemblies / csproj / TFM: ...
- NuGet / build: ...
- Hosting (Aspire/Kestrel/workers): ...
- Public surface (routes, events, CLI): ...
- Domain vs adapters: ...
- Events produced/consumed: ...
- Dependencies: ...
- Existing Rust crates (`native/`): ...

## Dependencies / blast radius
- Inbound: ...
- Outbound: ...
- Cross-cutting: ...
- Service-level migration notes: ...
- **Safety fact (first unit):** ...
  - Status: proven | unproven
  - Evidence: {command/result} or “assumption only”

## Recommended sequence (covers the service)
1. Unit: {vertical / island} — characterize → check: `{command}` → green means: ...
2. Unit: extract pure rules if embedded → check: `{command}` → green means: ...
3. Unit: **Rust port** (`native/<service>_<unit>/...`) → check: `cargo test` → green means: ...
4. Unit: **wire SERVICE to call Rust** → check: `{build + smoke}` → green means: Rust is on the live path
5. Unit: **parity harness** → check: `{harness from plan}` → green means: ...
6. Unit: {next service area from inventory} → ...
… (continue until inventoried service surface is planned)

## Risks
| Risk | Impact | Mitigation | Detection |
|------|--------|------------|-----------|
| ... | ... | ... | ... |

## First unit (first vertical to implement)
- Scope: characterize → extract (if needed) → Rust port → wire → parity (NOT extract-only; NOT “whole service done”)
- Why first: ...
- **Harness:** `{command or path}` — run before mass edits; fails closed on drift (service-specific script or dotnet/cargo from plan; e.g. `./scripts/check-catalog.sh` for Catalog)
- Rust crate path (proposed): `native/<service>_<unit>` (e.g. `native/catalog_stock`, `native/basket_totals`, etc.)
- Boundary: P/Invoke LibraryImport to cdylib (preferred) | Rust CLI for parity (acceptable if FFI too heavy)
- Acceptance: Rust on the path; characterization cases green against Rust-wired path; harness exit 0
- Remaining service coverage: see Recommended sequence units …
- Suggested tickets: ...
```

## Guardrails

- Direction is fixed: **.NET → Rust**. Input is the **service**, not a parameter table.  
- Scope the **entire service** the user points at. Catalog.API / eShop are examples only.  
- Definition of done = **whole-service migration plan** with sequenced units — not a first-slice-only artifact.  
- The first unit **must** land Rust + wiring + parity for its island. Extract-only is incomplete. Rust is required, not optional.  
- Prefer a **lever** (harness/script) before mass edits.  
- Prefer executable artifacts over slideware.  
- No fabricated LOC / % coverage / velocity — cite only what you measured.  
- Unproven safety facts block the first unit until proven or explicitly waived with owner + reason.
