# Handoff

## Completed
- [x] **T1** — solution scaffold & infrastructure (`b8beb79`)
- [x] **T2** — data model, `InitialSchema` migration, DB init (`b1b963c`)
- [x] **T3** — deterministic seed data + startup seeder (`a1fd2e4`)
- [x] **T4 — shared API infrastructure** (`938ee1e`, branch `feature/api-infrastructure`)
  - `Features/DashboardApi.cs`: `/api/dashboard` group + slice registration point (D8/D9)
  - `Shared/Period/`: `ReportingPeriod.Create` (validation + UTC bounds, D3/D4), `PeriodErrorCodes`, `PeriodQuery` (`[AsParameters]`), `DateRange`, `PeriodDto`
  - `Shared/Metrics/`: `MetricMath`, `ValueMetric` (decimal, money), `CountMetric` (int, count), `MarginMetric`
  - `Shared/Errors/`: `ErrorMapping.ToProblemResult` (Validation→400 w/ per-field + codes, NotFound→404, else 500), `GlobalExceptionHandler`, `ProblemDetailsRegistration` (traceId, D12)
  - `Shared/Json/AppJsonSerializerContext` (camelCase, explicit nulls, string enums); reflection left ON with a `TODO(T4)` in csproj (user decision)
  - OpenAPI (`AddOpenApi`/`MapOpenApi`) + Scalar UI in all envs; build-time document generation
  - Tests: period, formulas, error mapping, exception handler (throwing route via `IStartupFilter`, Production env), OpenAPI endpoint
  - `dotnet build` clean (0/0). Runtime-verified without DB: `/openapi/v1.json` → 200, `/scalar` → 200
  - Packages: `ErrorOr 2.1.1`, `Microsoft.AspNetCore.OpenApi 10.0.12`, `Scalar.AspNetCore 2.17.9`, `Microsoft.Extensions.ApiDescription.Server 10.0.12`

## Pending
- [ ] **OpenAPI output file name** — generated as `backend/openapi/SalesDashboard.Api.json` (tool default) but runtime route is `/openapi/v1.json`. Open question with user: rename output to `v1.json` in csproj, then commit the file (OPENAPI decision = commit it). NOT committed yet; `paths` is `{}` until slices exist. Waiting on user's yes to make the csproj rename.
- [ ] **T5–T6**: dashboard slices (`{Feature}Endpoint.Map(group)` on the `/api/dashboard` group); each will populate the OpenAPI document.
- [ ] **Run `dotnet test --solution SalesDashboard.slnx`** where Docker is available — not runnable here (WSL distro has no Docker; assembly-wide Testcontainers `PostgresFixture` blocks the entire suite, incl. pure unit tests). All tests compile.
- [ ] Not started: Dapper wiring, indexes (D11), frontend.
- [ ] Optional: restore `TreatWarningsAsErrors=true` (off since T1; user chose to keep false for T4 — build is 0-warning anyway).
- [ ] Not committed by design: `backend/openapi/`, and pre-existing edits to `.gitignore`/`Claude.md`/`docs/decisions.md` + untracked `.claude/`.

## Verify commands
```
cd backend
dotnet build SalesDashboard.slnx
dotnet test --solution SalesDashboard.slnx      # MTP: needs --solution/--project; needs Docker
```
Docker (not runnable in this WSL distro):
```
docker compose down -v && docker compose up --build   # seed log; then /scalar + /openapi/v1.json
curl http://localhost:8080/openapi/v1.json            # expect 200
```

## Learned (non-obvious)
- OpenAPI build-time generation works with no DB/env: `NpgsqlDataSource`/`DbContext` are lazy and `SalesDbInitializer.StartAsync` isn't invoked by the GetDocument tool (host built, not run). Same in Docker build stage.
- Meziantou `MA0002` fires on xunit asserts over framework string dictionaries (`ValidationProblemDetails.Errors`) with no followable comparer overload → disabled for `tests/**` in `.editorconfig` (mirrors existing `MA0004` exception).
- `GlobalExceptionHandler` integration test: inject the throwing route by appending middleware **after** `next(app)` in an `IStartupFilter` so it sits after `UseExceptionHandler` in the pipeline and the handler catches it; run the factory in Production to assert no stack trace leaks.
- Whole test suite is Testcontainers-based (assembly-level fixture) → even DB-free tests need Docker.
- `.NET 10` `dotnet test` (MTP): invoke with `--solution`/`--project`; bare path rejected.

## Context
- Branch: `feature/api-infrastructure` | Checkpoint: `938ee1e` | Base: `master`. Nothing pushed.
- Git identity: `dmitrii <dimatega@gmail.com>`. User-secrets `ConnectionStrings:Sales` set for local dev.
