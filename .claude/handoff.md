# Handoff

## Completed
- [x] **T1** — solution scaffold & infrastructure (`b8beb79`)
- [x] **T2** — data model, `InitialSchema` migration, DB init (`7f12683`)
- [x] **T3** — deterministic seed data + startup seeder (`42d13bf`)
- [x] **T4** — shared API infrastructure (`048c0ce`, merged into `master`)
- [x] **T5–T6 (checkpoint `678210d`)** — first two dashboard slices:
  - `Features/Kpi/`: `KpiEndpoint` + `KpiResponse`, `RefundsDto` (D1 refunds card), `BestManagerDto` (D7)
  - `Features/ManagerRanking/`: `RankingEndpoint`, `RankingRow`, `RankingResponse`, `RankingErrorCodes`; sports numbering (D7)
  - `Shared/Managers/`: `ManagerPerformanceReader`, `ManagerPerformanceRow`, `RankedManager`, `RankingMetric`, `CompetitionRanking`
  - `Shared/Metrics/`: `AverageCheckMetric` (D2), `MetricMath.Share` for refunds rate (D1/D6)
  - Both slices registered on `/api/dashboard` group; JSON source-gen context + OpenAPI doc updated
  - Dashboard integration tests: `KpiEndpointTests`, `RankingEndpointTests`, `TestDataBuilder`, `DashboardApp`/`DashboardTestHost`
  - `dotnet build` clean (0 warnings / 0 errors)

## Pending
- [ ] **Run the test suite** — build verified this checkpoint, tests NOT run. Needs Docker (assembly-level Testcontainers fixture blocks the whole suite, incl. DB-free tests). This WSL distro has no Docker.
- [ ] **CONSISTENCY-TEST** — KPI revenue == sum of ranking (and later categories/timeseries) once those slices exist.
- [ ] Remaining slices: timeseries (TIMESERIES), top-products (TOP-PRODUCTS), recent-sales (RECENT-SALES keyset), categories (CAT-COUNT).
- [ ] Dapper wiring for hot paths (D10), indexes (D11, `perf` branch), frontend (D14).

## Notes
- **`.gitignore` is dirty but must NOT be committed**: the diff is pure CRLF line-ending churn (494→494 lines, identical content). Left unstaged at this checkpoint.
- OpenAPI: generated file is `backend/openapi/SalesDashboard.Api.json` (tool default) while runtime route is `/openapi/v1.json`. It now has real paths (KPI + ranking) and is committed (OPENAPI decision).

## Verify commands
```
cd backend
dotnet build SalesDashboard.slnx
dotnet test --solution SalesDashboard.slnx      # MTP: needs --solution/--project; needs Docker
```

## Learned (non-obvious, still relevant)
- Whole test suite is Testcontainers-based (assembly-level fixture) → even DB-free tests need Docker to run.
- `.NET 10` `dotnet test` (MTP): invoke with `--solution`/`--project`; a bare path is rejected.
- OpenAPI build-time generation needs no DB/env: `NpgsqlDataSource`/`DbContext` are lazy and `SalesDbInitializer.StartAsync` isn't invoked by the GetDocument tool (host built, not run).

## Context
- Branch: `feature/dashboard-kpi-ranking` | Checkpoint: `678210d` | Base: `master`. Nothing pushed.
- Git identity: `dmitrii <dimatega@gmail.com>`. User-secrets `ConnectionStrings:Sales` set for local dev.
