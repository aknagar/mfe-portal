# Repository Health Report — mfe-portal

> **Last evaluated:** 2026-03-22
> **Evaluated by:** AI-assisted analysis of source code, test results, coverage reports, CI config, git history, and dependency manifests.

---

## Overview

This document tracks the health of the `mfe-portal` repository across 10 key metrics. Each metric includes a current-state assessment, concrete data, and recommended actions. The [Overall Health Summary](#overall-health-summary) at the bottom provides a quick-glance scorecard.

---

## Metric 1: Test Coverage

**What it measures:** The percentage of production code exercised by automated tests (line, branch, and method coverage).

### Current State

**Backend** (`backend/TestResults/CoverageReport/Summary.json`)

| Assembly | Line Coverage | Branch Coverage | Notes |
|---|---|---|---|
| AugmentService.Api | 78.5% (451/574) | 85.6% (113/132) | Endpoints at 0% |
| AugmentService.Application | 89.0% (138/155) | 80.0% (32/40) | DI class at 0% |
| AugmentService.Core | 69.4% (41/59) | 37.5% (6/16) | `PermissionPatternAttribute` at 0% |
| AugmentService.Infrastructure | 74.5% (302/405) | 65.3% (34/52) | DI, Extensions, DbInitializers at 0% |
| Dotnet.Utilities | **0%** (0/130) | **0%** (0/16) | Entirely untested |
| MfePortal.ServiceDefaults | **0%** (0/79) | **0%** (0/6) | Entirely untested |
| **TOTAL** | **67.3%** (1098/1630) | **73.1%** (215/294) | Method: 66.3% (128/193) |

**Frontend:** No coverage tooling configured. No jest/vitest. Only Playwright E2E tests exist — no unit or component coverage at all.

**CI thresholds:** 50% line coverage enforced; internal target in `backend/coverlet.runsettings` is 80%.

### Recommended Actions

- Add unit tests for `Dotnet.Utilities` and `MfePortal.ServiceDefaults` to eliminate the 0% assemblies
- Cover `PermissionPatternAttribute.IsValid()` — it has Crap Score 110 (see [Metric 8](#metric-8-cyclomatic-complexity))
- Configure vitest + coverage in the frontend shell (`frontend/shell/`)
- Raise the CI gate from 50% → 70% as coverage improves

---

## Metric 2: Build Success Rate

**What it measures:** The percentage of CI pipeline runs that complete successfully.

### Current State

No remote CI history is stored locally. The following is inferred from git commit history:

**February 14–15, 2026 — Instability burst:**
9 `fix:` commits pushed directly to `main` within 24 hours, all addressing broken CI pipelines:

| Commit | Subject |
|---|---|
| `788e24a` | Fix Azure Container Apps HTTP endpoint port configuration (#12) |
| `a2b0fc0` | fix: update applicationUrl in launchSettings.json |
| `519dffd` | fix: remove commented documentation from deployment steps |
| `42fc21a` | fix: update environment variable handling in infrastructure workflows |
| `0b836c5` | fix: refactor CI workflow to separate build and test jobs |
| `1bd54ba` | fix: enhance CI workflow separating unit/integration/E2E tests |
| `56ea745` | fix: allow continuation on error for test result download |
| `3e2d1b1` | fix: update pull request trigger to use event types |
| `92f6bd7` | fix: update CI workflows to ignore missing test result files |

**Since March 2026:** No CI fix commits. PRs #24–#28 all merged without accompanying pipeline fixes — pipeline appears stable.

**CI structure** (`.github/workflows/ci-backend.yml`): 6 parallel jobs — `build`, `unit-tests`, `integration-tests`, `e2e-tests`, `coverage`, `publish-results`.

### Recommended Actions

- Add a build status badge to `README.md` for at-a-glance visibility
- Store test result trends (e.g., via GitHub Actions summary artifacts) to track success rate over time

---

## Metric 3: Vulnerable Dependencies

**What it measures:** Whether any dependencies (direct or transitive) have known CVEs or security advisories.

### Current State

**Zero vulnerability scanning is configured anywhere in this repository:**

- No `.github/dependabot.yml` — Dependabot absent
- No `npm audit` step in any frontend CI workflow
- No `dotnet list package --vulnerable` in backend CI
- No Snyk, OWASP Dependency Check, or equivalent

**Frontend packages of concern** (version age, not confirmed CVEs):

| Package | Pinned Version | Risk |
|---|---|---|
| `react-router` / `react-router-dom` | `^5.3.4` | Two major versions behind (v7 current); v5 has known XSS-related advisories |
| `piral-cli-vite` | `latest` (unpinned) | Non-deterministic — any breaking or malicious version pulled silently |
| `lucide-react` | `^0.294.0` | ~180 minor versions behind; significant drift |

**Backend packages:** Managed centrally in `backend/Directory.Packages.props`. No audit output found in any CI artifact.

### Recommended Actions

- Add `.github/dependabot.yml` to enable automated dependency update PRs for both `npm` and `nuget` ecosystems
- Add `npm audit --audit-level=high` to `ci-frontend.yml`
- Add `dotnet list package --vulnerable` to `ci-backend.yml`
- Pin `piral-cli-vite` to a specific version instead of `latest`

---

## Metric 4: Outdated Dependencies

**What it measures:** How far behind the latest stable releases the project's dependencies are.

### Current State

**Frontend** (`frontend/shell/package.json`):

| Package | Current | Status |
|---|---|---|
| `react` / `react-dom` | `^18.2.0` | v19 available — one major behind |
| `react-router` / `react-router-dom` | `^5.3.4` | v7 current — **two major versions behind** |
| `lucide-react` | `^0.294.0` | ~180 minor versions behind current |
| `tailwindcss` | `^3.4.0` | v4 released — one major behind |
| `typescript` | `^5.3.2` | v5.7+ available — slightly behind |
| `piral-cli-vite` | `latest` | Unpinned — non-deterministic |
| `vite` | `^6.0.0` | Current |
| `@azure/msal-browser` | `^4.0.0` | Current |

**Backend** (`backend/Directory.Packages.props`):

| Package | Concern |
|---|---|
| `Microsoft.AspNetCore.App 9.0.0` | Referenced in a .NET 10 target project — version mismatch |
| Aspire packages | Updated to 13.1.2 on 2026-03-08; previously caused `TypeLoadException` in tests |

### Recommended Actions

- Migrate `react-router` from v5 → v7 (breaking change — plan a dedicated branch)
- Update `lucide-react` incrementally (check for icon renames between versions)
- Upgrade `tailwindcss` to v4 (has a migration CLI: `npx @tailwindcss/upgrade`)
- Resolve the `Microsoft.AspNetCore.App` version mismatch with the .NET 10 target

---

## Metric 5: Test Pass Rate

**What it measures:** The percentage of automated tests that pass consistently.

### Current State

Latest test run: **2026-03-21** (`backend/TestResults/test-results.trx`)

| Metric | Value |
|---|---|
| Total tests | 183 |
| Executed | 183 |
| Passed | **183 (100%)** |
| Failed | 0 |
| Skipped (NotExecuted) | **51** |

**All 183 executed tests pass. However, 51 tests are permanently skipped** and never counted in "executed":

| Count | Root Cause | Location |
|---|---|---|
| 17 | `DaprClient` / `DaprWorkflowClient` are `sealed` — cannot be mocked with NSubstitute | `ApprovalsControllerTests.cs` |
| 7 | NSubstitute cannot mock logger argument / exception setup | `LoggingBehaviorTests.cs`, `ProxyApplicationServiceTests.cs` |
| 6 | Rate limiting returns HTTP 500 instead of 429 | `RateLimitingIntegrationTests.cs` |
| 4 | Dapr Workflow concurrent collections error | `RateLimitingIntegrationTests.cs` |
| 4 | Aspire DCP executable missing in CI (E2E infra) | `RateLimitingE2eTests.cs` |
| 4 | `TypeLoadException` — Aspire.Hosting.Testing version mismatch | `AppHostTests.cs`, `IntegrationTests.cs` |
| 4 | SQLite does not support `SelectMany with Distinct` | `UserRoleRepositoryTests.cs` |
| 4 | Docker/Testcontainers timeout | Integration tests |
| 1 | NSubstitute `When/Do` exception setup failure | `DeleteForecastCommandHandlerTests.cs` |

### Recommended Actions

- Resolve the 17 sealed Dapr mock skips by switching to `Moq` with `Mock<T>` wrappers (branch `fix/dapr-moq-mocking` is in progress)
- Fix the rate-limiting tests returning 500 instead of 429
- Resolve the `TypeLoadException` by aligning `Aspire.Hosting.Testing` version
- Replace SQLite-incompatible `SelectMany with Distinct` query with a compatible alternative
- Adopt a policy: `[Skip]` annotations must include a linked GitHub issue to track resolution

---

## Metric 6: Flaky Test Rate

**What it measures:** The percentage of tests that produce non-deterministic pass/fail results.

### Current State

No formal flaky test tracking exists. Evidence of acknowledged flakiness:

| Signal | Location | Detail |
|---|---|---|
| E2E retries configured | `frontend/playwright.config.ts:7` | `retries: process.env.CI ? 2 : 0` — up to 2 retries in CI |
| Hardcoded delay | `navigation.spec.ts` | `waitForTimeout(1000)` |
| Hardcoded delay | `debug.spec.ts` | `waitForTimeout(2000)` |
| Trace on retry | `playwright.config.ts` | `trace: 'on-first-retry'` — confirms retries happen regularly |
| Timing-based skip | Integration tests | 2 tests skipped due to Docker/Testcontainers startup timeouts |
| Single CI worker | `playwright.config.ts` | `workers: 1` in CI — set to reduce race conditions |

**Estimated flaky rate:** Unquantified. Backend unit tests show no flakiness patterns. E2E layer has structural flakiness.

### Recommended Actions

- Replace `waitForTimeout` hardcoded delays with `waitForSelector` or `waitForResponse` assertions
- Set up a flaky test detection workflow (e.g., run tests N times on a schedule, flag inconsistent results)
- Move Docker-dependent integration tests behind a tag (`[Category("docker")]`) so they can be skipped in environments without Docker

---

## Metric 7: Code Duplication

**What it measures:** The percentage of code that is copy-pasted or structurally repeated across the codebase.

### Current State

No duplication detection tooling is configured (no jscpd, SonarCloud, NDepend, or ESLint duplication rules).

**Manually observed duplication patterns:**

| Location | Pattern | Impact |
|---|---|---|
| `ProductData.Extensions` + `WeatherData.Extensions` | `CreateProductDbIfNotExists` and `CreateWeatherDbIfNotExists` are structural duplicates (identical complexity=6, identical Crap Score=42) | Bug fix in one won't propagate to the other |
| All 6 Dapr Activity classes | Same `constructor + RunAsync` pattern with no shared abstract base class | Adding a new activity means copy-pasting boilerplate |
| `ApprovalsControllerTests.cs` (×17 skips) | Near-identical `[Skip("reason")]` + test body blocks | Maintenance overhead |
| Frontend E2E specs | Repeated `page.goto('/')` + `expect(page.locator('#app')).toBeVisible()` preamble without shared fixture | Each spec must be updated if the app selector changes |

### Recommended Actions

- Add `jscpd` to the frontend CI workflow for duplication measurement baseline
- Extract a shared abstract base for Dapr Activity classes
- Consolidate the DB extension methods into a generic `CreateDbIfNotExists<T>()` helper
- Create a shared Playwright fixture for the common E2E test setup

---

## Metric 8: Cyclomatic Complexity

**What it measures:** How many independent code paths exist in a function. Higher complexity = harder to test and maintain.

### Current State

Source: `backend/TestResults/CoverageReport/Summary.md` — ReportGenerator Risk Hotspots table.

| Assembly | Class | Method | Complexity | Crap Score | Coverage |
|---|---|---|---|---|---|
| AugmentService.Core | `PermissionPatternAttribute` | `IsValid(...)` | **10** | **110** | **0%** |
| AugmentService.Infrastructure | `ProductData.Extensions` | `CreateProductDbIfNotExists(...)` | 6 | 42 | 0% |
| AugmentService.Infrastructure | `WeatherData.Extensions` | `CreateWeatherDbIfNotExists(...)` | 6 | 42 | 0% |
| Common | `DomainCustomization` | `Customize(...)` | **26** | 26 | 93.4% |

> **Crap Score** = complexity² × (1 − coverage)³ + complexity. A Crap Score of **110** means the method is both highly complex and completely untested — the highest-risk method in the codebase.

`DomainCustomization.Customize()` has very high complexity (26) but is acceptably covered (93.4%), so its Crap Score is low.

**Frontend:** No ESLint `max-complexity` rule configured. Frontend complexity is entirely unmeasured.

### Recommended Actions

- Write tests for `PermissionPatternAttribute.IsValid()` to immediately reduce its Crap Score
- Refactor `IsValid()` to reduce cyclomatic complexity below 5 (extract validation rules into named methods)
- Add ESLint `complexity` rule to `frontend/.eslintrc` with a threshold of 10
- Run `dotnet-coverage` + ReportGenerator on every PR to catch new hotspots before merge

---

## Metric 9: PR Cycle Time

**What it measures:** The elapsed time from opening a pull request to merging it.

### Current State

22 pull requests identified from squash-merge commits in git history.

**Recent PRs (March 2026):**

| PR | Merged | Subject |
|---|---|---|
| #23 | 2026-03-08 | 002 try openspec |
| #24 | 2026-03-21 19:54 | additional tests |
| #25 | 2026-03-21 21:13 | Superpowers |
| #26 | 2026-03-21 21:22 | Feature/agent instructions |
| #27 | 2026-03-21 22:47 | Increase code coverage |
| #28 | 2026-03-21 22:27 | chore: upgrade Dapr .NET SDK to 1.17.5 |

**Key observations:**

- PRs #24–#28 all merged within a **3-hour window** — average cycle time under 2 hours
- This is a **solo developer** project (`Akash Nagar`) — no required code reviewers or approval gates
- Some PRs (#10, #11) were labeled simply "Dev" with no description
- Fast cycle time is a natural consequence of solo development, not a process problem

### Recommended Actions

- Add PR description templates (`.github/pull_request_template.md`) to encourage consistent documentation even for solo work
- Consider enabling branch protection rules on `main` to require CI to pass before merge (even without reviewers)
- Add conventional commit enforcement (e.g., `commitlint`) to keep the git history clean and parseable

---

## Metric 10: Stale Branches

**What it measures:** Branches that have been merged or abandoned but not deleted, cluttering the repository.

### Current State

**Local branches:**

| Branch | Last Commit | Status |
|---|---|---|
| `main` | Active | ✅ Keep |
| `fix/dapr-moq-mocking` | ~2026-03-21 | ✅ Active WIP |
| `devcontainer` | ~2026-03-21 | ⚠️ Unclear — not recently merged |
| `fix/unit-tests-coverage` | 2026-03-21 | 🗑️ Merged as PR #27 |
| `feature/upgrade-dapr-1.17.5` | 2026-03-21 | 🗑️ Merged as PR #28 |
| `increase-code-coverage` | 2026-03-21 | 🗑️ Merged |
| `feature/agent-instructions` | 2026-03-21 | 🗑️ Merged as PR #26 |
| `superpowers` | 2026-03-21 | 🗑️ Merged as PR #25 |
| `002-try-openspec` | 2026-03-08 | 🗑️ Merged as PR #23 |
| `aspire-update` | 2026-03-08 | 🗑️ Content in main |
| `load-test` | ~2026-02-15 | 🗑️ Merged as PR #22 |
| `dev` | ~2026-02-15 | 🗑️ Superseded |

**Remote branches (`origin/*`):**

| Branch | Status |
|---|---|
| `origin/main` | ✅ Keep |
| `origin/increase-code-coverage` | 🗑️ Merged — stale |
| `origin/superpowers` | 🗑️ Merged — stale |
| `origin/aspirify` | 🗑️ Superseded — stale (~7 weeks) |
| `origin/azd` | 🗑️ Superseded — stale (~7 weeks) |

**Count: 9+ stale local branches, 4 stale remote branches.**

### Recommended Actions

- Delete all branches marked 🗑️ above (safe — all content is in `main`)
- Enable **automatic branch deletion after PR merge** in GitHub repo settings (Settings → General → "Automatically delete head branches")
- Investigate `devcontainer` branch before deleting

---

## Overall Health Summary

| # | Metric | Rating | Finding |
|---|--------|--------|---------|
| 1 | Test Coverage | 🟡 Yellow | 67.3% backend (target: 80%); 0% frontend |
| 2 | Build Success Rate | 🟢 Green | Stable since March 2026 |
| 3 | Vulnerable Dependencies | 🔴 Red | No scanning configured — exposure unknown |
| 4 | Outdated Dependencies | 🟡 Yellow | React Router v5 is 2 major versions behind |
| 5 | Test Pass Rate | 🟡 Yellow | 100% pass rate, but 51 tests permanently skipped |
| 6 | Flaky Test Rate | 🟡 Yellow | E2E retries=2 and hardcoded delays present |
| 7 | Code Duplication | 🟡 Yellow | No tooling; structural duplication observed |
| 8 | Cyclomatic Complexity | 🟡 Yellow | `PermissionPatternAttribute.IsValid` Crap Score=110 |
| 9 | PR Cycle Time | 🟡 Yellow | <2h average; no review gate (solo project) |
| 10 | Stale Branches | 🔴 Red | 9+ stale local/remote branches need cleanup |

### Quick Wins (Low Effort, High Impact)

1. **Enable Dependabot** — add `.github/dependabot.yml` (5 minutes, eliminates the Red on Metric 3)
2. **Enable auto-delete branches on merge** — one GitHub settings toggle (eliminates the Red on Metric 10)
3. **Delete existing stale branches** — single `git branch -d` sweep
4. **Pin `piral-cli-vite`** to a specific version in `package.json`

### Longer-Term Investments

1. **Frontend test coverage** — configure vitest + coverage in `frontend/shell/`
2. **React Router v5 → v7 migration** — significant but high-value upgrade
3. **Resolve 51 skipped tests** — `fix/dapr-moq-mocking` is already tackling 17 of them
4. **Add jscpd / ESLint complexity rules** — to measure what is currently invisible
