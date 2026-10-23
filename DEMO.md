# Racetrac demo notes

This branch starts at upstream `dotnet/eShop` `main`. It adds only a cloud-agent environment, the catalog stock display, and audit/guardrail hooks. It does not include the fork's Rust migration, pstack plugin, or incident-response packs.

## Run the catalog tests

There is no `tests/Catalog.UnitTests` project on this tree. Catalog coverage lives in the functional suite.

```bash
dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
```

That suite starts Catalog.API against Postgres through Aspire. Docker must be running.

To compile the storefront slice first:

```bash
dotnet build eShop.Web.slnf
```

## Live issue to build in Plan Mode

Add `GET /api/catalog/items/low-stock` on Catalog.API.

- Query parameter `threshold`, default `10`
- Return catalog items whose `AvailableStock` is at or below that threshold
- Do not implement this endpoint before the live session

## What can break live

- Seed data sets every item to `AvailableStock = 100` and `RestockThreshold = 10`. A default threshold of 10 returns an empty list until you lower some stock or raise the threshold.
- catalog-api used to lose a `CREATE DATABASE` race with Aspire Postgres. `src/eShop.AppHost/AppHost.cs` now waits for `catalogDb` before the API starts. Do not drop that `WaitFor`.
- Cloud install no longer seeds a LibMan/cdnjs cache. A restricted network can fail restore of frontend libraries.
- `global.json` pins SDK `10.0.302`. The cloud Dockerfile installs the distro `dotnet-sdk-10.0` package. If that package is an earlier feature band such as `10.0.113`, `dotnet build` fails until you install `10.0.302`.
- A disposable `NUGET_PACKAGES` directory can bake stale paths into the storefront. After that, `_framework/blazor.web.js` 404s and the catalog grid stays on "Loading...".
- This branch has no `demo-prep` skill. Start the stack with `dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj` and open http://localhost:5045.
- Do not merge this pull request into the fork's `main`. That would delete the fork's demo packs.
