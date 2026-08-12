---
name: Migration validate
description: Use when validating that a migrated .NET→Rust unit worked — rank evidence honestly, prove blast-radius safety by running code, use check-catalog.sh and cargo/parity evidence, and decide keep/merge vs do-not-merge vs inconclusive.
---

# Migration validate

Gate after a migration unit. Prove the unit is safe enough to **keep and merge** (or roll back). Parameterize every run; no fake metrics. When fixing failures while validating, stop after **3** correction attempts — this skill remains the gate, not an infinite fix loop.

## What you need for this run

| Item | Meaning | Examples |
|------|---------|----------|
| Baseline | Pre-unit stack / behavior you must not regress | `.NET` Catalog domain before extract/port |
| After | Implementation under validation | extracted pure module **and** Rust island wired from .NET |
| Unit | Concrete change under validation | Catalog stock rules → `native/catalog_stock` + parity |
| Repo | Codebase path | local checkout |

Optional: CI job names, test commands, dual-run flag, rollback owner.

## When to use

- After implementing a unit via **Migrate to Rust** (from a **Scope .NET → Rust** whole-service plan)
- Before merging / demoing “unit done”
- When a team needs a repeatable keep-or-not gate
- When validating alongside iterative agent fixes (enforce the 3-attempt cap)

## Ideas this skill expects you to follow (explained here — no outside glossary)

- **Evidence ranking** — How strong is the claim?
  1. Self-report (“it works”) — weakest; never enough alone to keep/merge  
  2. Pointed at code — cited paths/diffs that *should* imply correctness  
  3. Ran real tests/commands — exit codes and output (**required floor to keep/merge**)  
  4. Optional runtime/deploy — dual-run, canary, or staging probe when warranted  
- **Safety fact proven by running code** — Restate the one blast-radius fact; prove it with a command, not a writeup.  
- **Keep / do-not-merge / inconclusive** — Explicit verdict: keep and merge the unit, do not merge it, or inconclusive (could not obtain required evidence). **Inconclusive is not keep/merge.**  
- **Stop after 3 fix attempts** — After the third failed correction, stop and hand residual risk to a human.  
- **Encode recurring failures** — Same failure class twice → add a test, script, or lint; do not grow prompt text.  
- **Rust on path for migrated units** — Keep/merge requires Rust implementation + parity evidence for units that were scoped to land in Rust — not a .NET extract alone.

## Evidence ranking (detail)

Rank claims by how they were obtained. Higher wins; never skip the floor for keep/merge:

1. **Self-report** — agent/human says it works (weakest; insufficient alone)
2. **Pointed at code** — cited paths/diffs that *should* imply correctness
3. **Ran real tests/commands** — exit codes and output from the repo’s suite/scripts (**required floor to keep/merge**)
4. **Optional runtime/deploy** — dual-run, canary, or staging probe when the unit warrants it

**Inconclusive ≠ keep/merge.** Missing commands, flaky runs, or “looks fine in the diff” without a green suite ⇒ **inconclusive** or **do not merge**, never pass.

## Artifact ladder (this repo — eShop)

Run in order for the unit; skip only with an explicit waiver + reason in the verdict:

1. **Characterization / unit + Rust** — For Catalog units, run `./scripts/check-catalog.sh` (committed lever; builds/tests Rust when `native/catalog_stock` exists). Otherwise run `dotnet test` on the project that covers the changed domain/API surface **and** `cargo test` for the Rust island. Record exit code and path to evidence (log, CI URL, or saved output).
2. **Catalog client-style evidence** — Prefer the same `./scripts/check-catalog.sh` path (unit then functional when Docker is available). If skipped: document why (no Docker, Aspire too heavy, unit not Catalog-touching) in `validate.md`.
3. **Optional runtime** — only if the stack is already running (Aspire AppHost / deployed env). Do not invent infra just for the demo.

**Keep/merge requirement for migrated units (Catalog inventory / stock and analogous):**

- Unit/characterization green via real commands — or an **explicit waiver** with owner + reason
- **Plus evidence that a Rust implementation exists and parity tests ran** — not just a .NET extract. Required evidence (cite paths + commands):
  - Rust crate present and built/tested (e.g. `native/catalog_stock`, `cargo test` / `./scripts/check-catalog.sh` Rust steps)
  - .NET→Rust boundary is live (wrapper / `LibraryImport` / documented CLI parity path) — Rust is not dead code
  - Same characterization cases exercised against the Rust-wired path (parity)
- A .NET-only extract with green C# tests is **insufficient** for keep/merge on a unit scoped to land in Rust.

Non-Catalog units: unit/characterization floor still required; use the harness from `plan.md`; Rust evidence applies when the scoped unit targeted a Rust island.

## Steps

1. **Freeze the claim**  
   One sentence: what behavioral guarantee this unit must hold. Example: “Catalog stock rules match pre-migration behavior for the characterized cases when Catalog.API uses the Rust-wired path.”

2. **Blast-radius proof (one safety fact)**  
   Identify the *one* safety fact this unit depends on (from scope `plan.md`, or restate it). **Prove it by running real code/script/tests** — not a writeup. Mark **proven** or **unproven**. Design docs and agent narration do not count. If unproven, verdict cannot be keep/merge until proven or explicitly waived with owner + reason.

3. **Characterization before / after + Rust parity**  
   - If tests did not exist: add characterization tests against **current baseline** behavior *before* structural change; record the command that runs them.  
   - After the unit: same tests must pass against the new structure / Rust-wired path.  
   - Prefer table-driven cases for pure rules; prefer contract/integration tests at adapter boundaries.  
   - Do not invent coverage percentages—report pass/fail and what was exercised.

4. **Walk the artifact ladder**  
   Execute the ladder above for this repo. Document exact commands, exit codes, and evidence paths. Prefer `./scripts/check-catalog.sh` and direct `cargo test` / parity output — do not invent ad-hoc HTTP probes unless needed. Explicitly record Rust + parity evidence for inventory/stock units.

5. **Optional fix-forward (companion, 3-attempt cap)**  
   If validating while an agent iterates on failures: allow at most **3** correction attempts. After the third failed attempt, **stop** — document failing commands, diffs tried, and residual risk for a human. Do not loop forever. This skill remains the gate; it does not own the fix loop.

6. **Encode recurring failures into structure**  
   If the same failure class appears twice (wrong TFM, missing Aspire dependency, flaky fixture, FFI load path): prefer a lint rule, script, narrow skill, or test harness over growing prompt text. Note the encoding action in `validate.md`.

7. **Rollback criteria**  
   Define objective triggers to revert or feature-flag off:
   - Characterization or parity suite failing on mainline CI  
   - Contract break with known consumers  
   - Undefined behavior / panic/crash in the new path under normal inputs  
   State rollback action (revert PR, toggle flag, restore previous artifact)—no drama, just steps.

8. **Verdict: keep/merge · do not merge · inconclusive**  
   Emit an explicit choice:
   - **Keep / merge** — claim holds; evidence ≥ “ran real tests”; blast-radius fact **proven** (or waived); for migrated Rust units, Rust implementation + parity evidence present; no open rollback triggers  
   - **Do not merge** — failed checks, broken parity, .NET-only extract without Rust on the path, unproven safety fact without waiver, or waived-without-owner gaps  
   - **Inconclusive** — could not obtain required evidence (env, missing suite, blocked run) — **not a pass; do not treat as keep/merge**  
   Every item evidence-based. Paste-ready for demo or PR.

9. **Emit artifact**  
    Write `validate.md` (or append a **Validation** section to the existing `plan.md`). Include commands actually run, evidence level, blast-radius fact status, attempt count if any, Rust/parity evidence (and `./scripts/check-catalog.sh` / cargo results), and the verdict—not aspirational metrics.

## Output: checklist template

```markdown
# Migration validate: {UNIT}

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
- [ ] Rust island + parity (migrated units): crate path: ... — `cargo test` / lever: ... — wired from .NET: yes | no — evidence: ...
- [ ] Optional runtime: ran | N/A — result: ...

## Parity
- [ ] Characterization tests exist and pass on baseline
- [ ] Same tests pass after unit (Rust-wired path)
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
```

## Guardrails

- Works for any baseline → after / unit; eShop Catalog is the primary demo surface, not the only valid target.  
- No fake pass-rates, LOC, or “% parity.”  
- Fail closed: missing characterization tests ⇒ do not merge until added or explicitly waived with reason.  
- Never treat **inconclusive** as **keep/merge**.  
- Migrated units keep/merge requires Rust implementation + parity evidence, not .NET extract alone.  
- Prefer encoding recurring failures into **structure** (lint rule, script, narrow skill) over growing a mega-prompt. For Catalog units, `./scripts/check-catalog.sh` is preferred command evidence when present.  
- Validation is the gate; fix-forward is optional and capped at 3 attempts.  
- Do not depend on removed skills (verify-catalog, migration-decision-trail, characterize-then-extract).
