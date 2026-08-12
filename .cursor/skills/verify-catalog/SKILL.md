---
name: Verify Catalog
description: Use when proving Catalog.API still behaves after a migration slice — drive the service the way a client would and capture evidence, including the Rust-wired path when present.
---

# Verify Catalog

Project-local verification skill for **eShop Catalog.API**. Written for cold agents who have never seen the app. Goal: prove Catalog still behaves after a migration slice by running real tests/commands and capturing exit codes — no fake metrics.

You do **not** need any external verification framework. Everything you need is in this file and this repo.

## When to use

- After a Catalog-related migration slice (characterize/extract, Rust port + wire, adapter change)
- When **Migration validate** requires the Verify Catalog rung of the artifact ladder
- Before claiming **keep/merge** on Catalog surface area

## How Catalog runs in this repo (discover, don’t invent)

| Path | What it is |
|------|------------|
| `src/Catalog.API/` | Catalog service (Kestrel API, EF + Postgres/pgvector, domain in `Model/`) |
| `src/eShop.AppHost/` | .NET Aspire AppHost that composes Catalog + dependencies |
| `tests/Catalog.FunctionalTests/` | HTTP functional tests via `WebApplicationFactory` + Aspire Postgres test resource |
| `tests/Catalog.UnitTests/` | Characterization / unit tests for domain (may be added by the slice) |
| `native/catalog_stock/` | Rust island for Catalog stock rules (`cdylib` + tests) when the migration slice has landed |
| `tests/README.md` | States functional tests need Docker (Aspire test containers) |
| `eShop.slnx` | Solution entry |

Domain rules historically live on `CatalogItem` in `src/Catalog.API/Model/CatalogItem.cs` (`RemoveStock` / `AddStock`, etc.). A migration slice may extract them and **wire callers through Rust** — when that path exists, verify through it.

## Preferred evidence (in order)

0. **`./scripts/check-catalog.sh`** — **first command when present**. Committed lever: builds/tests `native/catalog_stock` when present, then prefers `tests/Catalog.UnitTests` (no Docker), else `Catalog.FunctionalTests` when Docker is up, else exits 2 with a clear message. Capture its printed path + exit code as primary evidence. Use `MIGRATION_REQUIRE_RUST=1` when the slice claims Rust is required.
1. **Slice characterization / unit tests** (if the slice added them) — fastest, no Docker. Prefer cases that exercise the **Rust-wired** .NET wrapper / path when present (not only a leftover pure-.NET duplicate).  
2. **Rust crate tests** — `cargo test --manifest-path native/catalog_stock/Cargo.toml` when the crate exists.  
3. **`tests/Catalog.FunctionalTests`** — drives Catalog over HTTP the way a client would. Requires Docker because the fixture starts Postgres via Aspire.  
4. **Full Aspire AppHost** — only if already running or the demo environment supports it; do not stand up the whole estate just for smoke.

## Steps

1. **Resolve what the slice touched**  
   List changed paths under `src/Catalog.API/`, `native/`, and any new/updated test projects. Note whether HTTP surface, pure domain, Rust boundary, or both are in blast radius. If a Rust-wired path exists, treat it as in-scope for verification.

2. **Prefer existing harnesses**  
   Do **not** invent a new test host, docker-compose, or mock Catalog if `Catalog.FunctionalTests` or slice unit tests already cover the claim. Read `tests/Catalog.FunctionalTests/CatalogApiFixture.cs` and `CatalogApiTests.cs` before adding anything.

3. **Run the lightest honest smoke**

   **0. Lever (preferred when present)** — from repo root:

   ```bash
   ./scripts/check-catalog.sh
   # when the slice requires Rust:
   # MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh
   ```

   Record the script’s `path=` lines and `exit_code=`. If exit 0, you have Catalog smoke evidence (including Rust build/test when the crate exists); still escalate to functional/HTTP probes when the slice touches API/hosting contracts. If exit 2 (no unit project, no Docker), follow the script message — add tests via **Migrate slice to Rust**, or start Docker.

   **A. Minimal smoke (no Docker) — use when functional tests are too heavy**  
   If the slice added unit/characterization tests for Catalog domain (or you can run a focused project that does not need Aspire):

   ```bash
   # Prefer tests that hit the Rust-wired wrapper when present, e.g.:
   dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-restore
   cargo test --manifest-path native/catalog_stock/Cargo.toml
   ```

   If **no** Catalog unit project exists yet and the slice did not add one: you **cannot** invent green unit evidence. Either add characterization tests as part of the slice, escalate to functional tests (B), or return **inconclusive** / waiver to Migration validate — do not fabricate a suite.

   **B. Functional HTTP smoke (Docker + Aspire test host required)**  
   From repo root, with Docker running:

   ```bash
   dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
   ```

   Observe: process exit code `0`, xUnit assertions in `CatalogApiTests`. Save stdout/stderr or CI log path as evidence.

   **Escalate to B** when: the slice touches HTTP/API contracts, EF mappings, or Program hosting — or when A is unavailable.

   **C. Optional runtime (only if already up)**  
   If AppHost is already running locally:

   ```bash
   curl -sS -o /tmp/catalog-items.json -w "%{http_code}" \
     "http://localhost:<catalog-port>/api/catalog/items?pageIndex=0&pageSize=5"
   ```

   Record HTTP status + a short note on payload shape. Skip entirely if AppHost is not running.

4. **Observe — real signals only**  
   - Exit codes from `./scripts/check-catalog.sh`, `dotnet test`, `cargo test`  
   - Failed assertion names / messages  
   - HTTP status codes if probing a live host  
   - Confirmation that the Rust-wired path was exercised when present  
   Do **not** invent latency, coverage %, or “parity scores.”

5. **Checkable definition of done for this skill**  
   This skill is done when:
   - Exact commands run are listed  
   - Each command’s exit code (or skip + reason) is recorded  
   - Evidence paths are noted (log file, CI URL, or pasted summary)  
   - When Rust wiring is present: noted that verification went through that path (or explicitly why not)  
   - Result handed to **Migration validate**: green | red | skipped-with-reason

## Output template

```markdown
# Verify Catalog

## Slice context
- Touched: ...
- Rust-wired path present: yes | no — exercised: yes | no | N/A

## Commands
- [ ] `./scripts/check-catalog.sh` — path: ... — exit: ... — evidence: ...
- [ ] Rust (`cargo test` / lever R: steps): ran | N/A — exit: ... — evidence: ...
- [ ] `{command}` — exit: ... — evidence: ...
- [ ] Functional tests: ran | skipped (reason: ...) — exit: ... — evidence: ...
- [ ] Runtime probe: ran | N/A — result: ...

## Observations
- Key assertions / HTTP statuses: ...
- Parity / Rust path notes: ...

## Result for Migration validate
green | red | skipped-with-reason
```

## Guardrails

- Cold-agent friendly: use only paths and commands that exist in this repo (or that the slice just added).  
- Functional tests **require Docker**; if Docker/Aspire is unavailable, say so and fall back to A or skip with reason — do not pretend they passed.  
- Prefer `./scripts/check-catalog.sh` when present; then unit + Rust tests; then `Catalog.FunctionalTests` over inventing infra.  
- When the migration wired Catalog through Rust, verify through that path — do not only green a bypassed pure-.NET duplicate.  
- No fake metrics. Exit codes and assertion failures are the evidence.  
- Hand result back to **Migration validate**; append trail rows there (or via **Migration decision trail**), not as a substitute for the gate.
