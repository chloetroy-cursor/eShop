---
name: Migration decision trail
description: Use during migration scoping/implementation/validation to keep an append-only decisions.tsv audit trail.
---

# Migration decision trail

Append-only audit trail for migration work (scope → implement → validate). Inspired by show-your-work / decision-log patterns: every meaningful call gets a row with evidence, not a slide.

## When to use

- During **Scope .NET → Rust**, **implementation**, or **Migration validate**
- When a demo/PR needs to show *why* a Go/No-go was issued
- When multiple agents touch the same slice and need a shared history

## Default path

Prefer (create parent dirs if missing):

1. `migrations/decisions.tsv` — default for demo/PR confidence  
2. `.audit/migration-decisions.tsv` — alternative if the repo already uses `.audit/`

Commit the file when the demo or PR should show the trail. Template (header only):  
`.cursor/skills/migration-decision-trail/decisions-template.tsv`

## Format (TSV)

Columns (tab-separated, one header row, then append-only data rows):

| Column | Meaning |
|--------|---------|
| `ts` | ISO-8601 UTC timestamp |
| `phase` | `scope` \| `implement` \| `validate` \| other short label |
| `decision` | What was decided (short) |
| `why` | Rationale in one line |
| `evidence` | Command, path, PR URL, or log pointer |
| `result` | Outcome (`plan.md written`, `tests green`, `Go`, `No-go`, `Inconclusive`, …) |

Do **not** rewrite history. Correct mistakes with a new row that supersedes the prior decision.

## Steps

1. **Ensure file exists**  
   If missing, copy the template header:

   ```bash
   mkdir -p migrations
   cp .cursor/skills/migration-decision-trail/decisions-template.tsv migrations/decisions.tsv
   ```

2. **Append on the milestones**  
   Append **one row** at each of:
   - Scope finishes (`phase=scope`) — e.g. first slice chosen, safety fact proven/unproven  
   - Slice lands (`phase=implement`) — e.g. extract + characterization tests merged/ready  
   - Validate verdict (`phase=validate`) — `Go` / `No-go` / `Inconclusive` with evidence paths  

3. **Keep rows honest**  
   Evidence must point at something a reviewer can open (command + exit code, file path, CI URL). No fabricated metrics.

4. **Commit when needed**  
   Include `migrations/decisions.tsv` in the demo PR when confidence/audit is part of the story.

## Example rows

```tsv
ts	phase	decision	why	evidence	result
2026-08-12T00:00:00Z	scope	First slice = CatalogItem pure rules	I/O-free domain; harness-before-change	plan.md#first-demo-able-slice	plan.md written; safety fact unproven
2026-08-12T00:30:00Z	implement	Extract RemoveStock/AddStock + tests	Characterize before any Rust island	dotnet test tests/Catalog.UnitTests	tests green
2026-08-12T01:00:00Z	validate	Go	Unit + Verify Catalog green; safety fact proven	validate.md; migrations/decisions.tsv	Go
```

## Guardrails

- Append-only; never edit prior rows in place.  
- One row minimum at scope end, slice land, and validate verdict.  
- Tabs between columns; no commas-as-separators (values may contain commas).  
- Pair with **Migration validate** — the trail does not replace the gate.
