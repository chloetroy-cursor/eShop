---
name: Verify Catalog
description: Use when proving Catalog.API still behaves after a migration slice — drive the service the way a client would and capture evidence.
---

# Verify Catalog

Project-local verification skill for **eShop Catalog.API**. Written for cold agents who have never seen the app. Goal: prove Catalog still behaves after a migration slice by running real tests/commands and capturing exit codes — no fake metrics.

## When to use

- After a Catalog-related migration slice (extract/characterize, adapter change, or Rust island)
- When **Migration validate** requires the Verify Catalog rung of the artifact ladder
- Before claiming Go on Catalog surface area

## How Catalog runs in this repo (discover, don’t invent)

| Path | What it is |
|------|------------|
| `src/Catalog.API/` | Catalog service (Kestrel API, EF + Postgres/pgvector, domain in `Model/`) |
| `src/eShop.AppHost/` | .NET Aspire AppHost that composes Catalog + dependencies |
| `tests/Catalog.FunctionalTests/` | HTTP functional tests via `WebApplicationFactory` + Aspire Postgres test resource |
| `tests/README.md` | States functional tests need Docker (Aspire test containers) |
| `eShop.slnx` | Solution entry |

There is **no** dedicated `Catalog.UnitTests` project on `main` today. Domain rules live on `CatalogItem` in `src/Catalog.API/Model/CatalogItem.cs` (`RemoveStock` / `AddStock`, etc.). A migration slice may add characterization/unit tests — prefer those when present.

## Preferred evidence (in order)

1. **Slice characterization / unit tests** (if the slice added them) — fastest, no Docker.  
2. **`tests/Catalog.FunctionalTests`** — drives Catalog over HTTP the way a client would (`GetCatalogItemsRespectsPageSize`, update paths, etc.). Requires Docker because the fixture starts Postgres via Aspire.  
3. **Full Aspire AppHost** — only if already running or the demo environment supports it; do not stand up the whole estate just for smoke.

## Steps

1. **Resolve what the slice touched**  
   List changed paths under `src/Catalog.API/` and any new/updated test projects. Note whether HTTP surface, pure domain, or both are in blast radius.

2. **Prefer existing harnesses**  
   Do **not** invent a new test host, docker-compose, or mock Catalog if `Catalog.FunctionalTests` or slice unit tests already cover the claim. Read `tests/Catalog.FunctionalTests/CatalogApiFixture.cs` and `CatalogApiTests.cs` before adding anything.

3. **Run the lightest honest smoke**

   **A. Minimal smoke (no Docker) — use when functional tests are too heavy**  
   If the slice added unit/characterization tests for Catalog domain (or you can run a focused project that does not need Aspire):

   ```bash
   # Replace with the actual test project the slice introduced, e.g.:
   dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-restore
   # or filter within a known project:
   # dotnet test <project> --filter "FullyQualifiedName~CatalogItem"
   ```

   If **no** Catalog unit project exists yet and the slice did not add one: you **cannot** invent green unit evidence. Either add characterization tests as part of the slice, escalate to functional tests (B), or return **Inconclusive** / waiver to Migration validate — do not fabricate a suite.

   **B. Functional HTTP smoke (Docker + Aspire test host required)**  
   From repo root, with Docker running:

   ```bash
   dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
   ```

   Observe: process exit code `0`, xUnit assertions in `CatalogApiTests` (pagination count/pageSize, update-without-price-change, etc.). Save stdout/stderr or CI log path as evidence.

   **Escalate to B** when: the slice touches HTTP/API contracts, EF mappings, or Program hosting — or when A is unavailable.

   **C. Optional runtime (only if already up)**  
   If AppHost is already running locally:

   ```bash
   # Example client-style probe — adjust base URL/port from Aspire dashboard / launchSettings
   curl -sS -o /tmp/catalog-items.json -w "%{http_code}" \
     "http://localhost:<catalog-port>/api/catalog/items?pageIndex=0&pageSize=5"
   ```

   Record HTTP status + a short note on payload shape. Skip entirely if AppHost is not running.

4. **Observe — real signals only**  
   - Exit codes from `dotnet test`  
   - Failed assertion names / messages  
   - HTTP status codes if probing a live host  
   Do **not** invent latency, coverage %, or “parity scores.”

5. **Done predicate**  
   This skill is done when:
   - Exact commands run are listed  
   - Each command’s exit code (or skip + reason) is recorded  
   - Evidence paths are noted (log file, CI URL, or pasted summary)  
   - Result handed to **Migration validate**: green | red | skipped-with-reason

## Output template

```markdown
# Verify Catalog

## Slice context
- Touched: ...

## Commands
- [ ] `{command}` — exit: ... — evidence: ...
- [ ] Functional tests: ran | skipped (reason: ...) — exit: ... — evidence: ...
- [ ] Runtime probe: ran | N/A — result: ...

## Observations
- Key assertions / HTTP statuses: ...

## Result for Migration validate
green | red | skipped-with-reason
```

## Guardrails

- Cold-agent friendly: use only paths and commands that exist in this repo (or that the slice just added).  
- Functional tests **require Docker**; if Docker/Aspire is unavailable, say so and fall back to A or skip with reason — do not pretend they passed.  
- Prefer `Catalog.FunctionalTests` over inventing infra.  
- No fake metrics. Exit codes and assertion failures are the evidence.  
- Hand result back to **Migration validate**; append trail rows there (or via **Migration decision trail**), not as a substitute for the gate.
