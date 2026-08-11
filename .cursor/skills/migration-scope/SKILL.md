---
name: Migration scope
description: Use when scoping a codebase migration — inventory, blast radius, verifiable sequence, safety fact, and first harness-backed slice (e.g. .NET→Rust, Java modernization, COBOL→Java).
---

# Migration scope

Reusable playbook for scoping a brownfield migration slice. Parameterize every run; do not assume a specific customer, repo, or stack.

## Parameters

| Param | Meaning | Examples |
|-------|---------|----------|
| `SOURCE` | Current language/stack/version | `.NET Framework`, `.NET 8`, `Java 8`, `COBOL` |
| `TARGET` | Destination language/stack/version | `Rust`, `Go`, `.NET Core`, `Java 17` |
| `ENTRY` | Service, module, or bounded context to scope first | `Catalog.API`, `Payments`, `BatchSettlement` |
| `REPO` | Path or URL of the codebase | local checkout or fork |

Optional: team size, ticket system (e.g. Jira), LOC estimate, non-negotiable constraints (latency, regulatory, dual-run).

## When to use

- Starting a migration engagement or internal modernization wave
- Need inventory → blast radius → sequence → first safe slice before writing production code
- Consulting teams that must repeat the same scoping pattern across clients/stacks

## Steps

1. **Confirm parameters + falsifiable done predicate**  
   Record `SOURCE`, `TARGET`, `ENTRY`, `REPO`. State success criteria in one line (velocity of safe slices, not “rewrite everything”).  
   Then write a **done predicate** for *this scoping run* — a checkable claim that “scoped well” means, e.g.:
   - `plan.md` names inbound/outbound edges for `ENTRY` with local vs cross-cutting labels
   - Recommended sequence is ≥2 verifiable units, each ending in a green check
   - First slice lists a harness command that can fail before mass edits  
   The predicate must be falsifiable: someone can re-open the artifact and say pass/fail. No vibes.

2. **Inventory `ENTRY`**  
   - List projects/packages under `ENTRY` (APIs, domain, infra, tests).  
   - Note build system, runtime, package manager.  
   - Identify public surface: HTTP/gRPC endpoints, message consumers, CLI, batch jobs.  
   - Identify domain core vs adapters (DB, messaging, auth, third-party).  
   Write findings under **Inventory** in `plan.md`.

3. **Map dependencies & blast radius — find the one safety fact**  
   - Inbound: who calls `ENTRY` (other services, UI, jobs).  
   - Outbound: what `ENTRY` calls (DB schemas, brokers, shared libs).  
   - Shared types / contracts that would force multi-service changes.  
   - Test coverage hotspots and untested critical paths.  
   Label each edge: *local* (change stays in `ENTRY`) vs *cross-cutting*.  
   **Safety fact:** name the *one* fact the first slice depends on (e.g. “pure rules for X are I/O-free”, “endpoint Y has characterization coverage”, “schema Z is owned only by ENTRY”).  
   Mark it **proven** (you ran code/tests/commands that demonstrate it) or **unproven** (writeup/assumption only). Do not trust a design doc alone. If unproven, the first unit must prove it or the slice is blocked.  
   Write under **Dependencies / blast radius**.

4. **Propose migration sequence as verifiable units**  
   Prefer vertical slices that compile, test, and ship independently. Sequence must be **verifiable units** — each unit ends in a checkable green state before the next starts:
   1. Pure domain rules / pure functions (no I/O) — extract + characterize.  
   2. Ports/adapters behind stable interfaces.  
   3. Read paths before write paths when risk differs.  
   4. Dual-run or strangler only when contracts are stable.  
   For each unit: *change → check (command) → green*. No “then we refactor a lot” without a gate.  
   Avoid big-bang `SOURCE`→`TARGET` rewrites as the first move.  
   Write under **Recommended sequence**.

5. **Call out risks**  
   For each major risk: trigger, impact, mitigation, detection (test/metric/alert).  
   Typical themes (adapt, don’t invent numbers): behavioral drift, shared-kernel coupling, data-shape mismatch, missing characterization tests, ops/runtime skill gap for `TARGET`.  
   Write under **Risks**.

6. **Pick the first demo-able slice — build the lever first**  
   Choose the smallest slice that:
   - is mostly pure or well-bounded,
   - can gain characterization tests *before* behavior changes,
   - proves the playbook (scope → implement → validate),
   - does **not** require a full `TARGET` rewrite of `ENTRY`.  
   **Lever before mass edits:** prefer a characterization harness / script / codemod that makes the next N edits cheap and checkable. When behavior must be preserved, the first demo-able slice includes **harness-before-change** (harness exists and fails-closed on drift, then the structural edit).  
   Example pattern (not required): extract pure domain rules for an entity/aggregate, add tests, optionally port that island to `TARGET` later.  
   Write under **First demo-able slice** with acceptance checks, first-unit harness, and suggested ticket titles.

7. **Emit artifact**  
   Write `plan.md` at the workspace/migration root (or path the user specifies). Keep it actionable for an agent or human. No fake metrics. Re-check the done predicate before calling the run complete.

## Output: `plan.md` template

```markdown
# Migration scope: {ENTRY} ({SOURCE} → {TARGET})

## Done predicate
- [ ] ...
- How to falsify: ...

## Inventory
- ...

## Dependencies / blast radius
- Inbound: ...
- Outbound: ...
- Cross-cutting: ...
- **Safety fact:** ...
  - Status: proven | unproven
  - Evidence: {command/result} or “assumption only”

## Recommended sequence
1. Unit: ... → check: `{command}` → green means: ...
2. Unit: ... → check: `{command}` → green means: ...

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

- Skills must stay **general**: Intellias, eShop, Catalog.API, Brex are examples only.  
- Do not promise full rewrites in the live path; prefer extract → characterize → optional port.  
- Prefer building a **lever** (harness/script/codemod) before mass edits.  
- Prefer artifacts humans/agents can execute over slideware.  
- No fabricated LOC/% coverage/velocity numbers—cite only what you measured.  
- Unproven safety facts block the first slice until proven or explicitly waived with owner + reason.
