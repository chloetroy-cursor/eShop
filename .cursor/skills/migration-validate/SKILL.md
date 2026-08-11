---
name: Migration validate
description: Use when validating that a migration slice worked — tests, behavioral parity, and a go/no-go checklist.
---

# Migration validate

Reusable playbook to prove a migration slice is safe enough to keep (or roll back). Parameterize every run; no fake metrics.

## Parameters

| Param | Meaning | Examples |
|-------|---------|----------|
| `SOURCE` | Pre-migration stack/behavior baseline | `.NET 8` Catalog domain |
| `TARGET` | Post-slice implementation | extracted pure module, or Rust/Go island |
| `SLICE` | Concrete change under validation | `CatalogItem` pure rules + tests |
| `REPO` | Codebase path | local checkout |

Optional: CI job names, test commands, dual-run flag, rollback owner.

## When to use

- After implementing a scoped migration slice (see **Migration scope**)
- Before merging / demoing “slice done”
- When a consultant team needs a repeatable go/no-go gate

## Steps

1. **Freeze the claim**  
   One sentence: what behavioral guarantee this slice must hold. Example: “Pure `CatalogItem` pricing/availability rules match pre-extract behavior for the characterized cases.”

2. **Characterization before / after**  
   - If tests did not exist: add characterization tests against **current** `SOURCE` behavior *before* structural change; record the command that runs them.  
   - After the slice: same tests must pass against the new structure / `TARGET` island.  
   - Prefer table-driven cases for pure rules; prefer contract/integration tests at adapter boundaries.  
   - Do not invent coverage percentages—report pass/fail and what was exercised.

3. **Behavioral parity checks**  
   Choose checks that fit the slice (use what applies):
   - Unit: pure functions / domain rules — identical inputs → identical outputs / errors.  
   - API/contract: status codes, payloads, error shapes for touched endpoints.  
   - Data: migrations/backfills idempotent; no silent schema drift.  
   - Dual-run (only if already in plan): compare shadow results; define mismatch handling.  
   Document exact commands (e.g. `dotnet test`, `cargo test`, project-specific scripts).

4. **CI / local test commands**  
   List the minimal commands a reviewer runs:
   - Format/lint if required by the repo  
   - Unit/characterization suite for `SLICE`  
   - Broader suite if blast radius touched shared packages  
   Capture exit criteria: all listed commands green on a clean checkout.

5. **Rollback criteria**  
   Define objective triggers to revert or feature-flag off:
   - Characterization or parity suite failing on mainline CI  
   - Contract break with known consumers  
   - Undefined behavior / panic/crash in the new path under normal inputs  
   State rollback action (revert PR, toggle flag, restore previous artifact)—no drama, just steps.

6. **Go / no-go checklist**  
   Emit a short checklist (markdown) the demo or PR description can paste. Every item evidence-based.

7. **Emit artifact**  
   Write `validate.md` (or append a **Validation** section to the existing `plan.md`). Include commands actually run and results observed—not aspirational metrics.

## Output: checklist template

```markdown
# Migration validate: {SLICE}

## Claim
...

## Commands
- [ ] `{test command for slice}` — result: ...
- [ ] `{broader command if needed}` — result: ...

## Parity
- [ ] Characterization tests exist and pass on baseline
- [ ] Same tests pass after slice
- [ ] Contract/API checks (if applicable)

## Rollback
- Trigger: ...
- Action: ...

## Go / no-go
- [ ] Go — evidence: ...
- [ ] No-go — blockers: ...
```

## Guardrails

- Parameterized for any `SOURCE`→`TARGET` / `SLICE`; demo stacks are examples only.  
- No fake pass-rates, LOC, or “% parity.”  
- Fail closed: missing characterization tests ⇒ no-go until added or explicitly waived with reason.  
- Validation is part of the playbook, not a slide at the end.
