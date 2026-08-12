---
name: Migration validate
description: Use when validating that a migration slice worked (typically after a .NET→Rust scoped slice) — evidence ladder, real tests, and an explicit Go / No-go / Inconclusive verdict. Also fine for extract/characterize steps that are still on .NET.
---

# Migration validate

Reusable playbook to prove a migration slice is safe enough to keep (or roll back). Parameterize every run; no fake metrics. This skill is the **gate**; fix-forward is an optional companion pattern, not a third skill.

## Parameters

| Param | Meaning | Examples |
|-------|---------|----------|
| `SOURCE` | Pre-migration stack/behavior baseline | `.NET 8` Catalog domain |
| `TARGET` | Post-slice implementation | extracted pure module, or Rust/Go island |
| `SLICE` | Concrete change under validation | `CatalogItem` pure rules + tests |
| `REPO` | Codebase path | local checkout |

Optional: CI job names, test commands, dual-run flag, rollback owner.

## When to use

- After implementing a scoped migration slice (see **Scope .NET → Rust**; also usable for extract/characterize still on .NET)
- Before merging / demoing “slice done”
- When a consultant team needs a repeatable go/no-go gate
- When validating alongside iterative agent fixes (enforce the 3-attempt cap)

## Evidence ladder

Rank claims by how they were obtained. Higher wins; never skip the floor for a Go:

1. **Self-report** — agent/human says it works (weakest; insufficient alone)
2. **Pointed at code** — cited paths/diffs that *should* imply correctness
3. **Ran real tests/commands** — exit codes and output from the repo’s suite/scripts (**required floor for Go**)
4. **Optional runtime/deploy** — dual-run, canary, or staging probe when the slice warrants it

**Inconclusive ≠ Go.** Missing commands, flaky runs, or “looks fine in the diff” without a green suite ⇒ **Inconclusive** or **No-go**, never pass.

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
   Capture exit criteria: all listed commands green on a clean checkout. Record **evidence level** from the ladder for each.

5. **Optional fix-forward (companion, 3-attempt cap)**  
   If validating while an agent iterates on failures: allow at most **3** correction attempts. After the third failed attempt, **stop** — document failing commands, diffs tried, and residual risk for a human. Do not loop forever. This skill remains the gate; it does not own the fix loop.

6. **Rollback criteria**  
   Define objective triggers to revert or feature-flag off:
   - Characterization or parity suite failing on mainline CI  
   - Contract break with known consumers  
   - Undefined behavior / panic/crash in the new path under normal inputs  
   State rollback action (revert PR, toggle flag, restore previous artifact)—no drama, just steps.

7. **Verdict: Go / No-go / Inconclusive**  
   Emit an explicit enum:
   - **Go** — claim holds; evidence ≥ “ran real tests”; no open rollback triggers  
   - **No-go** — failed checks, broken parity, or waived-without-owner gaps  
   - **Inconclusive** — could not obtain required evidence (env, missing suite, blocked run) — **not a pass**  
   Every item evidence-based. Paste-ready for demo or PR.

8. **Emit artifact**  
   Write `validate.md` (or append a **Validation** section to the existing `plan.md`). Include commands actually run, evidence level, attempt count if any, and the verdict enum—not aspirational metrics.

## Output: checklist template

```markdown
# Migration validate: {SLICE}

## Claim
...

## Evidence level
self-report | pointed-at-code | ran-real-tests | runtime/deploy
(floor for Go: ran-real-tests)

## Commands
- [ ] `{test command for slice}` — result: ... — evidence: ...
- [ ] `{broader command if needed}` — result: ... — evidence: ...

## Parity
- [ ] Characterization tests exist and pass on baseline
- [ ] Same tests pass after slice
- [ ] Contract/API checks (if applicable)

## Fix-forward attempts (if any)
- Count: 0–3
- Stopped because: success | hit 3-attempt cap | N/A
- Notes for human (if capped): ...

## Rollback
- Trigger: ...
- Action: ...

## Verdict
- [ ] Go — evidence: ...
- [ ] No-go — blockers: ...
- [ ] Inconclusive — missing evidence: ...
(Inconclusive ≠ Go)
```

## Guardrails

- Parameterized for any `SOURCE`→`TARGET` / `SLICE`; demo stacks (eShop, Catalog, Brex) are examples only.  
- No fake pass-rates, LOC, or “% parity.”  
- Fail closed: missing characterization tests ⇒ No-go until added or explicitly waived with reason.  
- Never treat **Inconclusive** as **Go**.  
- Prefer encoding recurring failures into **structure** (lint rule, script, narrow skill) over growing a mega-prompt — modular skills beat one giant checklist.  
- Validation is the gate; fix-forward is optional and capped at 3 attempts.
