---
name: migration-program
description: Orchestrates an autonomous, evidence-gated .NET-to-Rust migration lifecycle through specialist subagents.
---

# Migration program orchestrator

Use this as the advanced entrypoint above the repository's three existing
migration skills. The input is one .NET service.

## Phase 1 — plan and challenge in parallel

Launch these two subagents in one parallel tool-call message:

- `migration-planner` applies `migration-scope` to inventory the whole service,
  map dependencies, and propose the smallest complete first Rust unit.
- `migration-validator` runs in **preflight mode** to define the evidence,
  harness, safety fact, and rollback conditions required to accept that unit.

Both are read-only in Phase 1. They return findings to the orchestrator; they do
not edit code or create planning files.

Synthesize only:

```text
Service
Why this first unit
Boundary and blast radius
Harness command
Safety fact: proven | unproven
Acceptance evidence
Main risk or disagreement
Next autonomous unit
```

If the safety fact and harness are credible, proceed to Phase 2. If either is
unproven, improve the harness or choose a smaller unit rather than asking for
permission to proceed on weak evidence.

## Phase 2 — implement

1. Persist the whole-service sequence to `plan.md`.
2. Launch `migration-implementer`, which applies `migrate-to-rust` to the first
   unit only: characterize, port, wire, and prove parity.
3. Keep each unit independently verifiable. Do not expand into a later unit
   until postflight accepts the current one.

## Phase 3 — independent gate

Launch `migration-validator` again in **postflight mode**. It applies
`migration-validate`, runs the approved harness, and returns exactly one:

- `keep / merge`
- `do not merge`
- `inconclusive`

The validator must not accept the implementer's self-report. Keep/merge requires
real test output, Rust on the live path, parity evidence, and a proven or
explicitly waived safety fact. Stop after three failed correction attempts.

On `keep / merge`, commit and push the unit, update the draft PR, and proceed to
the next planned unit. On `do not merge` or `inconclusive`, correct the same
unit or reduce its scope. Production deploys, merges, and external ticket
creation still require an explicit operator request.
