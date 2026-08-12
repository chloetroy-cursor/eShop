---
name: Migration validate
description: Use when validating that a migration slice worked (typically after a .NET→Rust scoped slice) — evidence ladder, blast-radius proof, verify-catalog for Catalog slices, and an explicit Go / No-go / Inconclusive verdict with a decisions.tsv row.
---

# Migration validate

Gate after a migration slice. Prove the slice is safe enough to keep (or roll back). Parameterize every run; no fake metrics. Fix-forward is an optional companion pattern capped at 3 attempts — this skill remains the gate.

## Parameters

| Param | Meaning | Examples |
|-------|---------|----------|
| `SOURCE` | Pre-migration stack/behavior baseline | `.NET` Catalog domain |
| `TARGET` | Post-slice implementation | extracted pure module, or Rust island |
| `SLICE` | Concrete change under validation | `CatalogItem` pure rules + tests |
| `REPO` | Codebase path | local checkout |

Optional: CI job names, test commands, dual-run flag, rollback owner, trail path (default `migrations/decisions.tsv`).

## When to use

- After implementing a scoped migration slice (see **Scope .NET → Rust**)
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

## Artifact ladder (this repo — eShop)

Run in order for the slice; skip only with an explicit waiver + reason in the verdict and trail:

1. **Characterization / unit for the slice** — `dotnet test` on the project that covers the changed domain/API surface (e.g. new Catalog unit tests, or the slice’s test project). Record exit code and path to evidence (log, CI URL, or saved output).
2. **Verify Catalog** — invoke the **Verify Catalog** skill (`.cursor/skills/verify-catalog`) for any Catalog-related slice. Prefer its documented smoke / `tests/Catalog.FunctionalTests` path. If skipped: document why (no Docker, Aspire too heavy for the environment, slice not Catalog-touching) in `validate.md` and the trail.
3. **Optional runtime** — only if the stack is already running (Aspire AppHost / deployed env). Do not invent infra just for the demo.

**Go requirement for Catalog-related slices:** unit/characterization green **and** Verify Catalog green — or an **explicit waiver** with owner + reason (env limitation, out-of-scope surface). Non-Catalog slices: unit/characterization floor still required; Verify Catalog N/A.

## Steps

1. **Freeze the claim**  
   One sentence: what behavioral guarantee this slice must hold. Example: “Pure `CatalogItem` pricing/availability rules match pre-extract behavior for the characterized cases.”

2. **Blast-radius proof (one safety fact)**  
   Identify the *one* safety fact this slice depends on (from scope `plan.md`, or restate it). **Prove it by running real code/script/tests** — not a writeup. Mark **proven** or **unproven**. Design docs and agent narration do not count. If unproven, verdict cannot be Go until proven or explicitly waived with owner + reason.

3. **Characterization before / after**  
   - If tests did not exist: add characterization tests against **current** `SOURCE` behavior *before* structural change; record the command that runs them.  
   - After the slice: same tests must pass against the new structure / `TARGET` island.  
   - Prefer table-driven cases for pure rules; prefer contract/integration tests at adapter boundaries.  
   - Do not invent coverage percentages—report pass/fail and what was exercised.

4. **Walk the artifact ladder**  
   Execute the ladder above for this repo. Document exact commands, exit codes, and evidence paths. For Catalog slices, invoke **Verify Catalog** rather than inventing ad-hoc HTTP probes.

5. **Optional fix-forward (companion, 3-attempt cap)**  
   If validating while an agent iterates on failures: allow at most **3** correction attempts. After the third failed attempt, **stop** — document failing commands, diffs tried, and residual risk for a human. Do not loop forever. This skill remains the gate; it does not own the fix loop.

6. **Encode recurring failures into structure**  
   If the same failure class appears twice (wrong TFM, missing Aspire prerequisite, flaky fixture): prefer a lint rule, script, narrow skill, or test harness over growing prompt text. Note the encoding action in the trail.

7. **Rollback criteria**  
   Define objective triggers to revert or feature-flag off:
   - Characterization or parity suite failing on mainline CI  
   - Contract break with known consumers  
   - Undefined behavior / panic/crash in the new path under normal inputs  
   State rollback action (revert PR, toggle flag, restore previous artifact)—no drama, just steps.

8. **Verdict: Go / No-go / Inconclusive**  
   Emit an explicit enum:
   - **Go** — claim holds; evidence ≥ “ran real tests”; blast-radius fact **proven** (or waived); Catalog slices also have Verify Catalog green (or waived); no open rollback triggers  
   - **No-go** — failed checks, broken parity, unproven safety fact without waiver, or waived-without-owner gaps  
   - **Inconclusive** — could not obtain required evidence (env, missing suite, blocked run) — **not a pass**  
   Every item evidence-based. Paste-ready for demo or PR.

9. **Append decision trail**  
   Using **Migration decision trail**, append one row to `migrations/decisions.tsv` (or `.audit/migration-decisions.tsv`) with phase=`validate`, the verdict, why, evidence paths, and result. Commit the trail when the demo/PR needs to show confidence.

10. **Emit artifact**  
    Write `validate.md` (or append a **Validation** section to the existing `plan.md`). Include commands actually run, evidence level, blast-radius fact status, attempt count if any, Verify Catalog result/waiver, and the verdict enum—not aspirational metrics.

## Output: checklist template

```markdown
# Migration validate: {SLICE}

## Claim
...

## Blast-radius safety fact
- Fact: ...
- Status: proven | unproven | waived
- Evidence: {command + exit code / path} or waiver owner+reason

## Evidence level
self-report | pointed-at-code | ran-real-tests | runtime/deploy
(floor for Go: ran-real-tests)

## Artifact ladder
- [ ] Characterization/unit: `{command}` — result: ... — evidence: ...
- [ ] Verify Catalog: ran | skipped (reason: ...) — result: ... — evidence: ...
- [ ] Optional runtime: ran | N/A — result: ...

## Parity
- [ ] Characterization tests exist and pass on baseline
- [ ] Same tests pass after slice
- [ ] Contract/API checks (if applicable)

## Fix-forward attempts (if any)
- Count: 0–3
- Stopped because: success | hit 3-attempt cap | N/A
- Notes for human (if capped): ...

## Structure encoding (if recurring failure)
- ...

## Rollback
- Trigger: ...
- Action: ...

## Verdict
- [ ] Go — evidence: ...
- [ ] No-go — blockers: ...
- [ ] Inconclusive — missing evidence: ...
(Inconclusive ≠ Go)

## Trail
- Appended row to migrations/decisions.tsv: yes | no
```

## Guardrails

- Parameterized for any `SOURCE`→`TARGET` / `SLICE`; eShop Catalog is the primary demo surface, not the only valid target.  
- No fake pass-rates, LOC, or “% parity.”  
- Fail closed: missing characterization tests ⇒ No-go until added or explicitly waived with reason.  
- Never treat **Inconclusive** as **Go**.  
- Catalog-related Go requires Verify Catalog green or explicit waiver.  
- Prefer encoding recurring failures into **structure** (lint rule, script, narrow skill) over growing a mega-prompt.  
- Validation is the gate; fix-forward is optional and capped at 3 attempts.  
- Always leave a trail row on verdict (see **Migration decision trail**).
