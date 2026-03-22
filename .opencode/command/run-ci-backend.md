---
description: Build the backend solution, run unit tests with code coverage, and display a tabular coverage report
---

Run the full backend CI pipeline locally: build the solution, execute unit tests with code coverage collection, generate a coverage report using ReportGenerator, and display a formatted tabular summary.

## Steps

### 1. Build the backend solution

```bash
dotnet build backend/MfePortal.Backend.sln --configuration Debug 2>&1
```

- If the build **fails**, stop immediately and report the errors. Do not proceed.
- Report the build result: succeeded/failed, number of warnings.

### 2. Run unit tests with coverage

Run unit tests only (skip Integration, E2E, and LoadTest categories — these require Docker/running services):

```bash
dotnet test backend/MfePortal.Backend.sln --no-build --configuration Debug --filter "Category!=LoadTest&Category!=E2E&Category!=Integration" --collect:"XPlat Code Coverage" --results-directory backend/TestResults --settings backend/coverlet.runsettings --verbosity minimal --logger "trx;LogFileName=test-results.trx" 2>&1
```

Capture the total test counts from the output: passed, failed, skipped, per-assembly.

### 3. Generate the coverage report

Check if ReportGenerator is installed, install if missing:

```bash
reportgenerator --version 2>&1 || dotnet tool install -g dotnet-reportgenerator-globaltool
```

Generate the report:

```bash
reportgenerator "-reports:backend/TestResults/**/coverage.cobertura.xml" "-targetdir:backend/TestResults/CoverageReport" "-reporttypes:JsonSummary;MarkdownSummaryGithub;Cobertura" "-verbosity:Warning" 2>&1
```

### 4. Parse coverage data

Read `backend/TestResults/CoverageReport/Summary.json`.

From the `summary` object, extract:
- `linecoverage` — overall line coverage %
- `branchcoverage` — overall branch coverage %
- `methodcoverage` — overall method coverage %
- `coveredlines` / `coverablelines` / `totallines`
- `coveredbranches` / `totalbranches`

From the `coverage.assemblies` array, for each assembly extract:
- `name`, `coverage` (line %), `coveredlines`, `coverablelines`, `branchcoverage`, `coveredbranches`, `totalbranches`, `methodcoverage`

### 5. Display the CI report

Output the report in this exact format (fill in real values from the data):

---

## Backend CI Report

### Build

| Status | Warnings |
|--------|----------|
| ✅ Succeeded | _N_ |

### Test Results

| Assembly | Passed | Skipped | Failed | Total |
|----------|--------|---------|--------|-------|
| AugmentService.Api.UnitTests | _N_ | _N_ | _N_ | _N_ |
| AugmentService.Application.UnitTests | _N_ | _N_ | _N_ | _N_ |
| AugmentService.Core.UnitTests | _N_ | _N_ | _N_ | _N_ |
| AugmentService.Infrastructure.UnitTests | _N_ | _N_ | _N_ | _N_ |
| **Total** | **_N_** | **_N_** | **_N_** | **_N_** |

> Integration, E2E, and Load tests were excluded (require Docker / running services).

### Coverage Summary

| Metric | Coverage | Covered | Total Coverable | Threshold | Status |
|--------|----------|---------|-----------------|-----------|--------|
| Line | _N_% | _N_ | _N_ | 80% | ✅ / ⚠️ |
| Branch | _N_% | _N_ | _N_ | 80% | ✅ / ⚠️ |
| Method | _N_% | _N_ | _N_ | 80% | ✅ / ⚠️ |

> ✅ = meets 80% threshold, ⚠️ = below threshold (from `backend/coverlet.runsettings`)

### Per-Assembly Coverage

| Assembly | Line % | Lines (cov/total) | Branch % | Branches (cov/total) | Method % |
|----------|--------|-------------------|----------|----------------------|----------|
| AugmentService.Api | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |
| AugmentService.Application | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |
| AugmentService.Core | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |
| AugmentService.Infrastructure | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |
| Dotnet.Utilities | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |
| MfePortal.ServiceDefaults | _N_% | _N_/_N_ | _N_% | _N_/_N_ | _N_% |

> Include all non-test assemblies from Summary.json. Sort by line coverage descending. Omit `Common` (it is a test helper project, not production code).

### Artifacts

| File | Path |
|------|------|
| Coverage HTML report | `backend/TestResults/CoverageReport/index.html` _(open in browser)_ |
| TRX test results | `backend/TestResults/test-results.trx` |
| Cobertura XML | `backend/TestResults/CoverageReport/Cobertura.xml` |
| JSON summary | `backend/TestResults/CoverageReport/Summary.json` |

---

## Notes

- **Skipped tests**: Some tests in Infrastructure.UnitTests are individually marked `[Fact(Skip = "...")]` — this is expected.
- **Coverage threshold**: 80% line + branch is defined in `backend/coverlet.runsettings`. It is advisory — coverlet does not fail the build if unmet.
- **To include integration tests**: Remove `&Category!=Integration` from the filter, but requires Docker running for Testcontainers.
- **Full HTML report**: Run `reportgenerator "-reports:backend/TestResults/**/coverage.cobertura.xml" "-targetdir:backend/TestResults/CoverageReport" "-reporttypes:Html;JsonSummary"` then open `backend/TestResults/CoverageReport/index.html`.
