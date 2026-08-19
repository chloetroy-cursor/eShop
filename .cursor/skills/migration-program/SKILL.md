---
name: migration-program
description: Orchestrates an advanced .NET-to-Rust migration lifecycle by composing the existing migration-scope, migrate-to-rust, and migration-validate skills through specialist subagents and human approval gates.
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
Approval required: yes
```

Stop for explicit human approval. Do not implement merely because the plan is
plausible.

## Phase 2 — implement only after approval

After explicit approval:

1. Persist the approved whole-service sequence to `plan.md`.
2. Launch `migration-implementer`, which applies `migrate-to-rust` to the first
   approved unit only: characterize, port, wire, and prove parity.
3. Do not expand into later units.

## Phase 3 — independent gate

Launch `migration-validator` again in **postflight mode**. It applies
`migration-validate`, runs the approved harness, and returns exactly one:

- `keep / merge`
- `do not merge`
- `inconclusive`

The validator must not accept the implementer's self-report. Keep/merge requires
real test output, Rust on the live path, parity evidence, and a proven or
explicitly waived safety fact. Stop after three failed correction attempts.

Never merge, deploy, create tickets, or proceed to another migration unit
without separate human approval.
