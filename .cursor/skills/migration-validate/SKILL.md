---
name: Migration validate
description: Use when validating that a migration slice worked (typically after a .NET→Rust scoped slice) — rank evidence honestly, prove blast-radius safety by running code, Verify Catalog for Catalog slices, and decide keep/merge vs do-not-merge vs inconclusive, with a decisions.tsv row.
---

# Migration validate

Gate after a migration slice. Prove the slice is safe enough to **keep and merge** (or roll back). Parameterize every run; no fake metrics. When fixing failures while validating, stop after **3** correction attempts — this skill remains the gate, not an infinite fix loop.

## What you need for this run

| Item | Meaning | Examples |
|------|---------|----------|
| Baseline | Pre-slice stack / behavior you must not regress | `.NET` Catalog domain before extract |
| After-slice | Implementation under validation | extracted pure module, or Rust island |
| Slice | Concrete change under validation | `CatalogItem` pure rules + tests |
| Repo | Codebase path | local checkout |

Optional: CI job names, test commands, dual-run flag, rollback owner, trail path (default `migrations/decisions.tsv`).

## When to use

- After implementing a scoped migration slice (see **Scope .NET → Rust**)
- Before merging / demoing “slice done”
- When a team needs a repeatable keep-or-not gate
- When validating alongside iterative agent fixes (enforce the 3-attempt cap)

## Ideas this skill expects you to follow (explained here — no outside glossary)

- **Evidence ranking** — How strong is the claim?
  1. Self-report (“it works”) — weakest; never enough alone to keep/merge  
  2. Pointed at code — cited paths/diffs that *should* imply correctness  
  3. Ran real tests/commands — exit codes and output (**required floor to keep/merge**)  
  4. Optional runtime/deploy — dual-run, canary, or staging probe when warranted  
- **Safety fact proven by running code** — Restate the one blast-radius fact; prove it with a command, not a writeup.  
- **Keep / do-not-merge / inconclusive** — Explicit verdict: keep and merge the slice, do not merge it, or inconclusive (could not obtain required evidence). **Inconclusive is not keep/merge.**  
- **Stop after 3 fix attempts** — After the third failed correction, stop and hand residual risk to a human.  
- **Encode recurring failures** — Same failure class twice → add a test, script, or lint; do not grow prompt text.  
- **Append-only trail** — One row in `migrations/decisions.tsv` for the verdict (see **Migration decision trail**).

## Evidence ranking (detail)

Rank claims by how they were obtained. Higher wins; never skip the floor for keep/merge:

1. **Self-report** — agent/human says it works (weakest; insufficient alone)
2. **Pointed at code** — cited paths/diffs that *should* imply correctness
3. **Ran real tests/commands** — exit codes and output from the repo’s suite/scripts (**required floor to keep/merge**)
4. **Optional runtime/deploy** — dual-run, canary, or staging probe when the slice warrants it

**Inconclusive ≠ keep/merge.** Missing commands, flaky runs, or “looks fine in the diff” without a green suite ⇒ **inconclusive** or **do not merge**, never pass.

## Artifact ladder (this repo — eShop)

Run in order for the slice; skip only with an explicit waiver + reason in the verdict and trail:

1. **Characterization / unit for the slice** — For Catalog slices, prefer `./scripts/check-catalog.sh` (committed lever) as command evidence when present; otherwise `dotnet test` on the project that covers the changed domain/API surface (e.g. new Catalog unit tests, or the slice’s test project). Record exit code and path to evidence (log, CI URL, or saved output).
2. **Verify Catalog** — invoke the **Verify Catalog** skill (`.cursor/skills/verify-catalog`) for any Catalog-related slice. Prefer `./scripts/check-catalog.sh` then its documented smoke / `tests/Catalog.FunctionalTests` path. If skipped: document why (no Docker, Aspire too heavy for the environment, slice not Catalog-touching) in `validate.md` and the trail.
3. **Optional runtime** — only if the stack is already running (Aspire AppHost / deployed env). Do not invent infra just for the demo.

**Keep/merge requirement for Catalog-related slices:** unit/characterization green **and** Verify Catalog green — or an **explicit waiver** with owner + reason (env limitation, out-of-scope surface). Non-Catalog slices: unit/characterization floor still required; Verify Catalog N/A.

## Steps

1. **Freeze the claim**  
   One sentence: what behavioral guarantee this slice must hold. Example: “Pure `CatalogItem` pricing/availability rules match pre-extract behavior for the characterized cases.”

2. **Blast-radius proof (one safety fact)**  
   Identify the *one* safety fact this slice depends on (from scope `plan.md`, or restate it). **Prove it by running real code/script/tests** — not a writeup. Mark **proven** or **unproven**. Design docs and agent narration do not count. If unproven, verdict cannot be keep/merge until proven or explicitly waived with owner + reason.

3. **Characterization before / after**  
   - If tests did not exist: add characterization tests against **current baseline** behavior *before* structural change; record the command that runs them.  
   - After the slice: same tests must pass against the new structure / after-slice implementation.  
   - Prefer table-driven cases for pure rules; prefer contract/integration tests at adapter boundaries.  
   - Do not invent coverage percentages—report pass/fail and what was exercised.

4. **Walk the artifact ladder**  
   Execute the ladder above for this repo. Document exact commands, exit codes, and evidence paths. For Catalog slices, invoke **Verify Catalog** rather than inventing ad-hoc HTTP probes.

5. **Optional fix-forward (companion, 3-attempt cap)**  
   If validating while an agent iterates on failures: allow at most **3** correction attempts. After the third failed attempt, **stop** — document failing commands, diffs tried, and residual risk for a human. Do not loop forever. This skill remains the gate; it does not own the fix loop.

6. **Encode recurring failures into structure**  
   If the same failure class appears twice (wrong TFM, missing Aspire dependency, flaky fixture): prefer a lint rule, script, narrow skill, or test harness over growing prompt text. Note the encoding action in the trail.

7. **Rollback criteria**  
   Define objective triggers to revert or feature-flag off:
   - Characterization or parity suite failing on mainline CI  
   - Contract break with known consumers  
   - Undefined behavior / panic/crash in the new path under normal inputs  
   State rollback action (revert PR, toggle flag, restore previous artifact)—no drama, just steps.

8. **Verdict: keep/merge · do not merge · inconclusive**  
   Emit an explicit choice (legacy labels Go / No-go / Inconclusive are fine in the trail if you also state the plain meaning):
   - **Keep / merge (Go)** — claim holds; evidence ≥ “ran real tests”; blast-radius fact **proven** (or waived); Catalog slices also have Verify Catalog green (or waived); no open rollback triggers  
   - **Do not merge (No-go)** — failed checks, broken parity, unproven safety fact without waiver, or waived-without-owner gaps  
   - **Inconclusive** — could not obtain required evidence (env, missing suite, blocked run) — **not a pass; do not treat as keep/merge**  
   Every item evidence-based. Paste-ready for demo or PR.

9. **Append decision trail**  
   Using **Migration decision trail**, append one row to `migrations/decisions.tsv` (or `.audit/migration-decisions.tsv`) with phase=`validate`, the verdict, why, evidence paths, and result. Commit the trail when the demo/PR needs to show confidence.

10. **Emit artifact**  
    Write `validate.md` (or append a **Validation** section to the existing `plan.md`). Include commands actually run, evidence level, blast-radius fact status, attempt count if any, Verify Catalog result/waiver, and the verdict—not aspirational metrics.

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
(floor for keep/merge: ran-real-tests)

## Artifact ladder
- [ ] Characterization/unit (prefer `./scripts/check-catalog.sh` for Catalog): `{command}` — result: ... — evidence: ...
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
- [ ] Keep / merge — evidence: ...
- [ ] Do not merge — blockers: ...
- [ ] Inconclusive — missing evidence: ...
(Inconclusive ≠ keep/merge)

## Trail
- Appended row to migrations/decisions.tsv: yes | no
```

## Guardrails

- Works for any baseline → after-slice / slice; eShop Catalog is the primary demo surface, not the only valid target.  
- No fake pass-rates, LOC, or “% parity.”  
- Fail closed: missing characterization tests ⇒ do not merge until added or explicitly waived with reason.  
- Never treat **inconclusive** as **keep/merge**.  
- Catalog-related keep/merge requires Verify Catalog green or explicit waiver.  
- Prefer encoding recurring failures into **structure** (lint rule, script, narrow skill) over growing a mega-prompt. For Catalog slices, `./scripts/check-catalog.sh` is preferred command evidence when present.  
- Validation is the gate; fix-forward is optional and capped at 3 attempts.  
- Always leave a trail row on verdict (see **Migration decision trail**).
