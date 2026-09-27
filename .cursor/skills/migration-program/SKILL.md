---
name: migration-program
description: Orchestrates an autonomous, evidence-gated .NET-to-Rust migration lifecycle through specialist subagents.
---

# Migration program orchestrator

Use this as the advanced entrypoint above the repository's three existing
migration skills. The input is one .NET service, usually named by a Jira ticket
(for example ME-1: Catalog.API, first unit `RemoveStock` / `AddStock`). Read the
ticket first and treat its service, first unit, crate path, and harness as the
scope.

## Phase 0 — environment preflight

Check before launching subagents so Phase 1 reports real findings, not toolchain
gaps:

- `cargo --version`. If missing, install it: `curl --proto '=https' --tlsv1.2
  -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal` (reversible local
  tool install; proceed without asking). New shells pick it up from
  `~/.zshenv`; in the current shell prefix commands with
  `export PATH="$HOME/.cargo/bin:$PATH"`. `./scripts/check-catalog.sh` already
  does this itself.
- `dotnet --version` (10.x) and whether the unit test project the harness prefers
  exists (Catalog: `tests/Catalog.UnitTests`). Without it the harness falls back
  to the Docker functional suite, which is slow and has failures unrelated to the
  unit. The implementer creates the unit project as its first step; do not treat
  the missing project as a blocker.
- `cargo test --manifest-path native/Cargo.toml --workspace` on the baseline.

Shell calls that run cargo, dotnet, or the harness must run outside the sandbox.

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
permission to proceed on weak evidence. A safety fact that is `unproven` only
because no characterization suite exists yet is expected at this point; the
implementer proves it in its first step.

## Phase 2 — implement

1. Persist the whole-service sequence to `plan.md` at the repo root.
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
real test output, Rust on the live path (negative proof: remove the cdylib and
the suite fails), parity evidence, and a proven or explicitly waived safety
fact. The validator writes `validate.md` at the repo root. Stop after three
failed correction attempts.

On `keep / merge`, review the diff yourself, commit the unit together with
`plan.md` and `validate.md`, push, open a draft PR that lists the commands and
exit codes, and proceed to the next planned unit. On `do not merge` or `inconclusive`, correct the same
unit or reduce its scope. Production deploys, merges, and external ticket
creation still require an explicit operator request.
