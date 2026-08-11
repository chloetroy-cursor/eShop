---
name: Migration scope
description: Use when scoping a codebase migration — inventory, blast radius, sequencing, and risk for moving from one language/stack/version to another (e.g. .NET→Rust, Java modernization, COBOL→Java).
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

1. **Confirm parameters**  
   Record `SOURCE`, `TARGET`, `ENTRY`, `REPO`. State the success criteria in one line (e.g. velocity of safe slices, not “rewrite everything”).

2. **Inventory `ENTRY`**  
   - List projects/packages under `ENTRY` (APIs, domain, infra, tests).  
   - Note build system, runtime, package manager.  
   - Identify public surface: HTTP/gRPC endpoints, message consumers, CLI, batch jobs.  
   - Identify domain core vs adapters (DB, messaging, auth, third-party).  
   Write findings under **Inventory** in `plan.md`.

3. **Map dependencies & blast radius**  
   - Inbound: who calls `ENTRY` (other services, UI, jobs).  
   - Outbound: what `ENTRY` calls (DB schemas, brokers, shared libs).  
   - Shared types / contracts that would force multi-service changes.  
   - Test coverage hotspots and untested critical paths.  
   Label each edge: *local* (change stays in `ENTRY`) vs *cross-cutting*.  
   Write under **Dependencies / blast radius**.

4. **Propose migration sequence (small verticals first)**  
   Prefer vertical slices that compile, test, and ship independently:
   1. Pure domain rules / pure functions (no I/O) — extract + characterize.  
   2. Ports/adapters behind stable interfaces.  
   3. Read paths before write paths when risk differs.  
   4. Dual-run or strangler only when contracts are stable.  
   Avoid big-bang `SOURCE`→`TARGET` rewrites as the first move.  
   Write under **Recommended sequence**.

5. **Call out risks**  
   For each major risk: trigger, impact, mitigation, detection (test/metric/alert).  
   Typical themes (adapt, don’t invent numbers): behavioral drift, shared-kernel coupling, data-shape mismatch, missing characterization tests, ops/runtime skill gap for `TARGET`.  
   Write under **Risks**.

6. **Pick the first demo-able slice**  
   Choose the smallest slice that:
   - is mostly pure or well-bounded,
   - can gain characterization tests *before* behavior changes,
   - proves the playbook (scope → implement → validate),
   - does **not** require a full `TARGET` rewrite of `ENTRY`.  
   Example pattern (not required): extract pure domain rules for an entity/aggregate, add tests, optionally port that island to `TARGET` later.  
   Write under **First demo-able slice** with acceptance checks and suggested ticket titles.

7. **Emit artifact**  
   Write `plan.md` at the workspace/migration root (or path the user specifies). Keep it actionable for an agent or human. No fake metrics.

## Output: `plan.md` template

```markdown
# Migration scope: {ENTRY} ({SOURCE} → {TARGET})

## Inventory
- ...

## Dependencies / blast radius
- Inbound: ...
- Outbound: ...
- Cross-cutting: ...

## Recommended sequence
1. ...
2. ...

## Risks
| Risk | Impact | Mitigation | Detection |
|------|--------|------------|-----------|
| ... | ... | ... | ... |

## First demo-able slice
- Scope: ...
- Why first: ...
- Acceptance: ...
- Suggested tickets: ...
```

## Guardrails

- Skills must stay **general**: Intellias, eShop, Catalog.API are examples only.  
- Do not promise full rewrites in the live path; prefer extract → characterize → optional port.  
- Prefer artifacts humans/agents can execute over slideware.  
- No fabricated LOC/% coverage/velocity numbers—cite only what you measured.
