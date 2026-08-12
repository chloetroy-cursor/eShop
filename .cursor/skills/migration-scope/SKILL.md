---
name: Scope .NET → Rust
description: Use when scoping a .NET → Rust migration for one service the user points at (or named in a ticket). Inventory, blast radius, verifiable sequence, safety fact, first harness-backed slice.
---

# Scope .NET → Rust

Playbook for scoping a **.NET → Rust** brownfield migration slice for **one service**. Tickets already describe the work — the user points at a service; you scope it. Do not ask for SOURCE/TARGET.

**Fixed stack:** `SOURCE = .NET` · `TARGET = Rust`

## Input

The **service** (or project) the user points at — folder, project name, ticket link, or @ mention.

If unclear, infer once from the open ticket / chat / workspace context (e.g. Catalog.API in eShop). Do **not** interrogate for SOURCE/TARGET/ENTRY/REPO. If still ambiguous after one inference pass, ask only: *which .NET service?*

Optional context (take if offered, don’t block on it): constraints (latency, dual-run, regulatory), ticket system, Aspire/hosting notes.

## When to use

- User (or ticket) names a .NET service to migrate toward Rust
- Need inventory → blast radius → sequence → first safe slice before production edits
- Demo or engagement where the stack is already .NET → Rust

## Steps

1. **Resolve service + falsifiable done predicate**  
   Name the service (`SERVICE`). Success = velocity of *safe* slices, not “rewrite the estate.”  
   Write a **done predicate** for *this scoping run* — checkable, e.g.:
   - `plan.md` names inbound/outbound edges for `SERVICE` with local vs cross-cutting labels
   - Recommended sequence is ≥2 verifiable units, each ending in a green check
   - First slice lists a harness command that can fail before mass edits  
   Someone must be able to reopen the artifact and say pass/fail. No vibes.

2. **Inventory `SERVICE` (.NET-aware)**  
   - Assemblies / projects under the service: API, Domain, Infrastructure, tests (`.csproj`, solution filters).  
   - TFM(s), SDK-style vs legacy, NuGet / Central Package Management, build entrypoints.  
   - Hosting & boundaries: Aspire AppHost, Kestrel endpoints, gRPC, workers, message consumers.  
   - Public surface: HTTP/gRPC routes, events, CLI/batch.  
   - Domain core vs adapters (EF/DB, brokers, auth, third-party).  
   Write under **Inventory** in `plan.md`.

3. **Map dependencies & blast radius — one safety fact**  
   - Inbound: who calls `SERVICE` (other services, UI, jobs, Aspire references).  
   - Outbound: DB schemas, brokers, shared .NET libs/contracts.  
   - Shared types that would force multi-service changes.  
   - Test hotspots and untested critical paths (`dotnet test` projects).  
   Label each edge: *local* vs *cross-cutting*.  
   **Safety fact:** the *one* fact the first slice depends on (e.g. “CatalogItem pricing rules are I/O-free”, “endpoint Y has characterization coverage”, “schema Z owned only by SERVICE”).  
   Mark **proven** (you ran code/tests/commands) or **unproven** (assumption only). Design docs don’t count. If unproven, the first unit must prove it or the slice is blocked.  
   Write under **Dependencies / blast radius**.

4. **Propose .NET → Rust sequence as verifiable units**  
   Prefer vertical slices that compile, test, and ship independently. Each unit: *change → check (command) → green* before the next.
   Typical progression (adapt; don’t invent metrics):
   1. Pure .NET domain rules — extract + characterize (`dotnet test`).  
   2. Ports/adapters behind stable contracts.  
   3. Read paths before write paths when risk differs.  
   4. Rust island options as *sequence ideas* (pick what fits; don’t prescribe one architecture): crate behind FFI, gRPC sidecar, process boundary, or strangler dual-run — only when contracts are stable.  
   Avoid big-bang .NET→Rust rewrite of `SERVICE` as the first move.  
   Write under **Recommended sequence**.

5. **Call out risks**  
   For each major risk: trigger, impact, mitigation, detection (test/metric/alert).  
   Themes to adapt (no fake numbers): behavioral drift, shared-kernel / NuGet coupling, serialization & TFM quirks, missing characterization tests, Rust ops/runtime skill gap, FFI/ABI or sidecar deploy complexity.  
   Write under **Risks**.

6. **Pick the first demo-able slice — harness before change**  
   Smallest slice that:
   - is mostly pure or well-bounded in .NET,
   - can gain characterization tests *before* behavior changes,
   - proves scope → implement → validate,
   - does **not** require rewriting all of `SERVICE` in Rust.  
   **Harness-before-change:** characterization harness / script that fails closed on drift, then the structural edit. Optional later: port that island to a Rust crate.  
   Example (not required): extract pure CatalogItem rules + tests; Rust port is a later unit.  
   Write under **First demo-able slice** with acceptance checks, harness command, suggested ticket titles.

7. **Emit artifact**  
   Write `plan.md` at the workspace/migration root (or path the user specifies). Actionable for agent or human. No fake metrics. Re-check the done predicate before calling the run complete.

## Output: `plan.md` template

```markdown
# .NET → Rust migration scope: {SERVICE}

## Done predicate
- [ ] ...
- How to falsify: ...

## Inventory
- Assemblies / csproj / TFM: ...
- NuGet / build: ...
- Hosting (Aspire/Kestrel/workers): ...
- Public surface: ...
- Domain vs adapters: ...

## Dependencies / blast radius
- Inbound: ...
- Outbound: ...
- Cross-cutting: ...
- **Safety fact:** ...
  - Status: proven | unproven
  - Evidence: {command/result} or “assumption only”

## Recommended sequence
1. Unit: ... → check: `{dotnet test ... or other}` → green means: ...
2. Unit: ... → check: `{command}` → green means: ...
   (Later units may introduce Rust via crate / FFI / gRPC sidecar / strangler — choose explicitly.)

## Risks
| Risk | Impact | Mitigation | Detection |
|------|--------|------------|-----------|
| ... | ... | ... | ... |

## First demo-able slice
- Scope: ...
- Why first: ...
- **First unit harness:** `{command or path}` — run before mass edits; fails closed on drift
- Acceptance: ...
- Suggested tickets: ...
```

## Guardrails

- Stack is fixed: **.NET → Rust**. Input is the **service**, not a parameter table.  
- Catalog.API / eShop are examples only — any .NET service the user points at.  
- Do not promise a full Rust rewrite in the live path; prefer extract → characterize → optional Rust island.  
- Prefer a **lever** (harness/script) before mass edits.  
- Prefer executable artifacts over slideware.  
- No fabricated LOC / % coverage / velocity — cite only what you measured.  
- Unproven safety facts block the first slice until proven or explicitly waived with owner + reason.
