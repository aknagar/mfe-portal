#!/usr/bin/env bash
# =============================================================================
# run-ci.sh - CI - Backend (Local Runner)
#
# Mirrors the ci-backend.yml GitHub Actions workflow locally:
#   Stage 1  Build        - dotnet restore + build (Release)
#   Stage 2  Unit Tests   - fast feedback, no Docker required
#   Stage 3  Integration  - Testcontainers (requires Docker)
#   Stage 4  E2E Tests    - Aspire.Hosting.Testing (requires Docker + Aspire + Dapr)
#   Stage 5  Coverage     - full suite with Cobertura report + threshold check
#
# PR-only steps (GitHub comments, artifact uploads, check annotations) are omitted.
#
# USAGE
#   ./scripts/run-ci.sh [OPTIONS]
#
# OPTIONS
#   --stage <stage>         Run a single stage: build|unit|integration|e2e|coverage|all (default: all)
#   --skip-build            Skip the build stage (assumes Release binaries exist)
#   --skip-unit             Skip unit tests
#   --skip-integration      Skip integration tests (when Docker is unavailable)
#   --skip-e2e              Skip E2E tests (when Aspire/Dapr are not installed)
#   --skip-coverage         Skip the coverage stage
#   --no-build              Pass --no-build to dotnet test (skip implicit rebuild)
#   --threshold <pct>       Minimum line coverage % to enforce (default: 50)
#   --open-report           Open the HTML coverage report in a browser when done
#   -h, --help              Show this help and exit
#
# EXAMPLES
#   ./scripts/run-ci.sh
#       Full pipeline (build + unit + integration + e2e + coverage)
#
#   ./scripts/run-ci.sh --stage unit
#       Build then run unit tests only
#
#   ./scripts/run-ci.sh --skip-integration --skip-e2e
#       Build + unit + coverage (no Docker required)
#
#   ./scripts/run-ci.sh --skip-build --skip-unit --skip-integration --skip-e2e
#       Coverage stage only (assumes binaries already built)
#
#   ./scripts/run-ci.sh --threshold 80
#       Full pipeline, enforce 80% line coverage
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BACKEND_DIR="$REPO_ROOT/backend"
SOLUTION_FILE="$BACKEND_DIR/MfePortal.Backend.sln"
TEST_RESULTS_DIR="$BACKEND_DIR/TestResults"
COVERAGE_DIR="$TEST_RESULTS_DIR/CoverageReport"
RUN_SETTINGS="$BACKEND_DIR/coverlet.runsettings"

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
STAGE="all"
SKIP_BUILD=false
SKIP_UNIT=false
SKIP_INTEGRATION=false
SKIP_E2E=false
SKIP_COVERAGE=false
NO_BUILD=false
COVERAGE_THRESHOLD=50
OPEN_REPORT=false

# ---------------------------------------------------------------------------
# Colour helpers
# ---------------------------------------------------------------------------
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; GRAY='\033[0;37m'; RESET='\033[0m'

header()  { echo; printf "${CYAN}%s\n  %s\n%s${RESET}\n" "$(printf '=%.0s' {1..60})" "$1" "$(printf '=%.0s' {1..60})"; }
step()    { echo; printf "${YELLOW}>> %s${RESET}\n" "$1"; }
ok()      { printf "${GREEN}   %s${RESET}\n" "$1"; }
fail()    { printf "${RED}   %s${RESET}\n" "$1"; }

die() { fail "$1"; exit 1; }

# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --stage)            STAGE="$2";               shift 2 ;;
        --skip-build)       SKIP_BUILD=true;           shift ;;
        --skip-unit)        SKIP_UNIT=true;            shift ;;
        --skip-integration) SKIP_INTEGRATION=true;     shift ;;
        --skip-e2e)         SKIP_E2E=true;             shift ;;
        --skip-coverage)    SKIP_COVERAGE=true;        shift ;;
        --no-build)         NO_BUILD=true;             shift ;;
        --threshold)        COVERAGE_THRESHOLD="$2";   shift 2 ;;
        --open-report)      OPEN_REPORT=true;          shift ;;
        -h|--help)
            sed -n '/^# USAGE/,/^# ===*/p' "${BASH_SOURCE[0]}" | head -n -1 | sed 's/^# \?//'
            exit 0
            ;;
        *) die "Unknown option: $1 (run with --help for usage)" ;;
    esac
done

# Apply --stage shortcut (same logic as PowerShell version)
if [[ "$STAGE" != "all" ]]; then
    SKIP_BUILD=true; SKIP_UNIT=true; SKIP_INTEGRATION=true; SKIP_E2E=true; SKIP_COVERAGE=true
    case "$STAGE" in
        build)       SKIP_BUILD=false ;;
        unit)        SKIP_UNIT=false;        [[ "$NO_BUILD" == "true" ]] || SKIP_BUILD=false ;;
        integration) SKIP_INTEGRATION=false; [[ "$NO_BUILD" == "true" ]] || SKIP_BUILD=false ;;
        e2e)         SKIP_E2E=false;         [[ "$NO_BUILD" == "true" ]] || SKIP_BUILD=false ;;
        coverage)    SKIP_COVERAGE=false;    [[ "$NO_BUILD" == "true" ]] || SKIP_BUILD=false ;;
        all)         SKIP_BUILD=false; SKIP_UNIT=false; SKIP_INTEGRATION=false; SKIP_E2E=false; SKIP_COVERAGE=false ;;
        *) die "Unknown stage: $STAGE. Valid: build|unit|integration|e2e|coverage|all" ;;
    esac
fi

NO_BUILD_FLAG=""
if [[ "$NO_BUILD" == "true" ]] || [[ "$SKIP_BUILD" == "true" ]]; then
    NO_BUILD_FLAG="--no-build"
fi

# ---------------------------------------------------------------------------
# Banner
# ---------------------------------------------------------------------------
header "CI - Backend (Local Runner)"
echo
printf "${GRAY}  Repo:      %s${RESET}\n" "$REPO_ROOT"
printf "${GRAY}  Backend:   %s${RESET}\n" "$BACKEND_DIR"
printf "${GRAY}  Stage:     %s${RESET}\n" "$STAGE"
printf "${GRAY}  Threshold: %s%%${RESET}\n" "$COVERAGE_THRESHOLD"
echo
printf "${GRAY}  Stages to run:${RESET}\n"
[[ "$SKIP_BUILD"       == "true" ]] && printf "${GRAY}    Build       : SKIP${RESET}\n" || echo "    Build       : YES"
[[ "$SKIP_UNIT"        == "true" ]] && printf "${GRAY}    Unit Tests  : SKIP${RESET}\n" || echo "    Unit Tests  : YES"
[[ "$SKIP_INTEGRATION" == "true" ]] && printf "${GRAY}    Integration : SKIP${RESET}\n" || echo "    Integration : YES"
[[ "$SKIP_E2E"         == "true" ]] && printf "${GRAY}    E2E         : SKIP${RESET}\n" || echo "    E2E         : YES"
[[ "$SKIP_COVERAGE"    == "true" ]] && printf "${GRAY}    Coverage    : SKIP${RESET}\n" || echo "    Coverage    : YES"

# ---------------------------------------------------------------------------
# Prerequisite checks
# ---------------------------------------------------------------------------
header "Checking Prerequisites"

command -v dotnet >/dev/null 2>&1 || die "dotnet not found. Install .NET 10 SDK: https://dotnet.microsoft.com/download"
ok "dotnet $(dotnet --version)"

if [[ "$SKIP_INTEGRATION" == "false" ]] || [[ "$SKIP_E2E" == "false" ]] || [[ "$SKIP_COVERAGE" == "false" ]]; then
    command -v docker >/dev/null 2>&1 || die "docker not found. Install Docker Desktop: https://www.docker.com/products/docker-desktop/"
    docker info >/dev/null 2>&1 || die "Docker daemon is not running. Start Docker Desktop and retry."
    ok "Docker is running"
fi

if [[ "$SKIP_E2E" == "false" ]]; then
    command -v dapr >/dev/null 2>&1 || die "dapr CLI not found. See: https://docs.dapr.io/getting-started/install-dapr-cli/ then run: dapr init"
    ok "dapr: $(dapr --version 2>&1 | grep 'CLI version' | tr -d ' ')"

    dotnet workload list 2>/dev/null | grep -q aspire || \
        die ".NET Aspire workload not installed. Run: dotnet workload install aspire"
    ok ".NET Aspire workload installed"
fi

[[ -f "$SOLUTION_FILE" ]] || die "Solution file not found: $SOLUTION_FILE"
ok "Solution: $SOLUTION_FILE"

# ---------------------------------------------------------------------------
# Stage 1 - Build
# ---------------------------------------------------------------------------
if [[ "$SKIP_BUILD" == "false" ]]; then
    header "Stage 1/5 - Build"

    step "Restore NuGet packages"
    dotnet restore "$SOLUTION_FILE"

    step "Build solution (Release)"
    dotnet build "$SOLUTION_FILE" --no-restore --configuration Release

    ok "Build succeeded."
else
    header "Stage 1/5 - Build [SKIPPED]"
fi

# ---------------------------------------------------------------------------
# Stage 2 - Unit Tests
# ---------------------------------------------------------------------------
if [[ "$SKIP_UNIT" == "false" ]]; then
    header "Stage 2/5 - Unit Tests"

    UNIT_RESULTS="$TEST_RESULTS_DIR/unit"
    rm -rf "$UNIT_RESULTS"

    step "Run unit tests (no Docker required)"
    dotnet test "$SOLUTION_FILE" \
        --configuration Release \
        --filter "Category!=Integration&Category!=E2E&Category!=LoadTest" \
        --logger "trx;LogFileName=$UNIT_RESULTS/unit-test-results.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$UNIT_RESULTS" \
        $NO_BUILD_FLAG

    ok "Unit tests passed."
else
    header "Stage 2/5 - Unit Tests [SKIPPED]"
fi

# ---------------------------------------------------------------------------
# Stage 3 - Integration Tests  (requires Docker / Testcontainers)
# ---------------------------------------------------------------------------
if [[ "$SKIP_INTEGRATION" == "false" ]]; then
    header "Stage 3/5 - Integration Tests (Testcontainers)"

    INTEGRATION_RESULTS="$TEST_RESULTS_DIR/integration"
    rm -rf "$INTEGRATION_RESULTS"

    step "Run integration tests (Docker required for Testcontainers)"
    dotnet test "$SOLUTION_FILE" \
        --configuration Release \
        --filter "Category=Integration" \
        --logger "trx;LogFileName=$INTEGRATION_RESULTS/integration-test-results.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$INTEGRATION_RESULTS" \
        $NO_BUILD_FLAG

    ok "Integration tests passed."
else
    header "Stage 3/5 - Integration Tests [SKIPPED]"
fi

# ---------------------------------------------------------------------------
# Stage 4 - E2E Tests  (requires Docker + Aspire + Dapr)
# ---------------------------------------------------------------------------
if [[ "$SKIP_E2E" == "false" ]]; then
    header "Stage 4/5 - E2E Tests (Aspire.Hosting.Testing)"

    E2E_RESULTS="$TEST_RESULTS_DIR/e2e"
    rm -rf "$E2E_RESULTS"

    step "Run E2E tests (Docker + Aspire + Dapr required)"
    dotnet test "$SOLUTION_FILE" \
        --configuration Release \
        --filter "Category=E2E" \
        --logger "trx;LogFileName=$E2E_RESULTS/e2e-test-results.trx" \
        --logger "console;verbosity=normal" \
        --results-directory "$E2E_RESULTS" \
        $NO_BUILD_FLAG

    ok "E2E tests passed."
else
    header "Stage 4/5 - E2E Tests [SKIPPED]"
fi

# ---------------------------------------------------------------------------
# Stage 5 - Coverage
# ---------------------------------------------------------------------------
if [[ "$SKIP_COVERAGE" == "false" ]]; then
    header "Stage 5/5 - Code Coverage"

    COVERAGE_RESULTS="$TEST_RESULTS_DIR/coverage"
    rm -rf "$COVERAGE_RESULTS" "$COVERAGE_DIR"

    SETTINGS_ARGS=""
    [[ -f "$RUN_SETTINGS" ]] && SETTINGS_ARGS="--settings $RUN_SETTINGS"

    step "Run full test suite with XPlat Code Coverage (excludes LoadTests)"
    dotnet test "$SOLUTION_FILE" \
        --configuration Release \
        --filter "Category!=LoadTest" \
        --collect:"XPlat Code Coverage" \
        --results-directory "$COVERAGE_RESULTS" \
        --verbosity normal \
        --logger "trx;LogFileName=$COVERAGE_RESULTS/test-results.trx" \
        $NO_BUILD_FLAG \
        $SETTINGS_ARGS

    # Install ReportGenerator if missing
    if ! dotnet tool list -g 2>/dev/null | grep -q 'dotnet-reportgenerator-globaltool'; then
        step "Install ReportGenerator (dotnet global tool)"
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi

    # Collect coverage XML files
    mapfile -t COVERAGE_FILES < <(find "$COVERAGE_RESULTS" -name 'coverage.cobertura.xml')
    [[ ${#COVERAGE_FILES[@]} -eq 0 ]] && die "No coverage.cobertura.xml files found in $COVERAGE_RESULTS"

    COVERAGE_PATHS=$(IFS=';'; echo "${COVERAGE_FILES[*]}")

    step "Generate HTML + Cobertura + JSON coverage report"
    reportgenerator \
        "-reports:$COVERAGE_PATHS" \
        "-targetdir:$COVERAGE_DIR" \
        "-reporttypes:Html;HtmlSummary;Cobertura;JsonSummary;Badges;MarkdownSummary" \
        "-verbosity:Warning"

    SUMMARY_JSON="$COVERAGE_DIR/Summary.json"
    [[ -f "$SUMMARY_JSON" ]] || die "Coverage summary not found: $SUMMARY_JSON"

    # Parse summary (jq mirrors the CI parsing)
    LINE_COVERAGE=$(jq -r '.summary.linecoverage' "$SUMMARY_JSON")
    BRANCH_COVERAGE=$(jq -r '.summary.branchcoverage' "$SUMMARY_JSON")
    METHOD_COVERAGE=$(jq -r '.summary.methodcoverage // "N/A"' "$SUMMARY_JSON")

    echo
    printf "${CYAN}%s\n  Coverage Summary\n%s${RESET}\n" "$(printf '=%.0s' {1..60})" "$(printf '=%.0s' {1..60})"
    echo
    printf "${YELLOW}  Line Coverage:   ${RESET}"
    awk -v val="$LINE_COVERAGE" -v thr="$COVERAGE_THRESHOLD" \
        'BEGIN { printf (val+0 >= thr+0) ? "\033[0;32m%.2f%%\033[0m\n" : "\033[0;31m%.2f%%\033[0m\n", val }'
    printf "${YELLOW}  Branch Coverage: ${RESET}"
    awk -v val="$BRANCH_COVERAGE" -v thr="$COVERAGE_THRESHOLD" \
        'BEGIN { printf (val+0 >= thr+0) ? "\033[0;32m%.2f%%\033[0m\n" : "\033[1;33m%.2f%%\033[0m\n", val }'
    printf "${YELLOW}  Method Coverage: ${RESET}%s\n" "$METHOD_COVERAGE%"
    echo
    printf "${GRAY}  Threshold: %s%%${RESET}\n" "$COVERAGE_THRESHOLD"
    printf "${GRAY}  Report:    %s/index.html${RESET}\n" "$COVERAGE_DIR"

    # Open report in browser if requested
    if [[ "$OPEN_REPORT" == "true" ]]; then
        REPORT_INDEX="$COVERAGE_DIR/index.html"
        if [[ -f "$REPORT_INDEX" ]]; then
            step "Opening coverage report in browser..."
            if command -v xdg-open >/dev/null 2>&1; then
                xdg-open "$REPORT_INDEX" &
            elif command -v open >/dev/null 2>&1; then
                open "$REPORT_INDEX"
            fi
        fi
    fi

    # Enforce threshold (mirrors CI behaviour: exits 1 if below)
    awk -v val="$LINE_COVERAGE" -v thr="$COVERAGE_THRESHOLD" 'BEGIN {
        if (val+0 < thr+0) {
            printf "\033[0;31m\n%s\n  COVERAGE BELOW THRESHOLD\n%s\033[0m\n", \
                "============================================================", \
                "============================================================"
            printf "  Required : %s%%\n  Actual   : %.2f%%\n\n", thr, val
            exit 1
        }
    }' || exit 1

    ok "Coverage meets threshold ($LINE_COVERAGE% >= $COVERAGE_THRESHOLD%)."
else
    header "Stage 5/5 - Code Coverage [SKIPPED]"
fi

# ---------------------------------------------------------------------------
# Done
# ---------------------------------------------------------------------------
echo
printf "${GREEN}%s\n  ALL STAGES PASSED\n%s${RESET}\n\n" \
    "$(printf '=%.0s' {1..60})" "$(printf '=%.0s' {1..60})"
exit 0
