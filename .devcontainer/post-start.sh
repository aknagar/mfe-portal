#!/bin/bash
set -e

readonly ASPIRE_LOG="/tmp/aspire.log"

echo "Running post-start setup..."

# Ensure dotnet global tools (aspire, etc.) are on PATH
export PATH="$HOME/.dotnet/tools:$HOME/.dapr/bin:$PATH"

# ---------------------------------------------------------------------------
# Print a formatted table of Aspire resource states
# ---------------------------------------------------------------------------
print_resource_table() {
    local response="$1"
    echo ""
    echo "+------------------------------+----------------------+"
    echo "|       Aspire Resource Status                        |"
    echo "+------------------------------+----------------------+"
    printf "| %-28s | %-20s |\n" "Resource" "State"
    echo "+------------------------------+----------------------+"

    # Extract name+state pairs using python3 (reliable JSON parsing, no jq needed)
    local pairs
    pairs=$(echo "$response" | python3 -c "
import json, sys
data = json.load(sys.stdin)
resources = data.get('resources', [])
for r in resources:
    name = r.get('name', '?')
    state = r.get('state', '?')
    print(name + '\t' + state)
" 2>/dev/null)

    echo "$pairs" | while IFS=$'\t' read -r name state; do
        if [ -z "$name" ]; then continue; fi
        local icon="  "
        case "$state" in
            Running|Finished|Exited) icon="+ " ;;
            FailedToStart|RuntimeUnhealthy) icon="X " ;;
            Starting|Building|Waiting) icon=". " ;;
        esac
        printf "| %-28s | %s%-18s |\n" "$name" "$icon" "$state"
    done
    echo "+------------------------------+----------------------+"
    echo ""
}

# ---------------------------------------------------------------------------
# Poll Aspire Resource Service API until all resources are healthy
# ---------------------------------------------------------------------------
wait_for_aspire_resources() {
    local dashboard_url="${1:-https://localhost:15001}"
    local max_wait="${2:-300}"
    local pid="${3:-}"
    local waited=0
    local response=""

    echo ""
    echo "Waiting for all Aspire resources to become healthy (up to ${max_wait}s)..."

    while [ $waited -lt $max_wait ]; do
        # Check if AppHost process is still alive
        if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then
            echo "ERROR: Aspire AppHost process exited during resource startup. See $ASPIRE_LOG"
            return 1
        fi

        response=$(curl -fsSk "${dashboard_url}/api/v1/resources" 2>/dev/null)

        if [ -z "$response" ]; then
            waited=$((waited + 5))
            sleep 5
            continue
        fi

        local pending healthy failed total
        pending=$(echo "$response" | grep -oP '"state"\s*:\s*"(Starting|Building|Waiting|NotStarted)"' | wc -l)
        failed=$(echo "$response"  | grep -oP '"state"\s*:\s*"(FailedToStart|RuntimeUnhealthy)"'       | wc -l)
        healthy=$(echo "$response" | grep -oP '"state"\s*:\s*"(Running|Finished|Exited)"'              | wc -l)
        total=$(echo "$response"   | grep -oP '"state"\s*:\s*"[^"]+"'                                  | wc -l)

        if [ "$total" -eq 0 ]; then
            echo "  Waiting for Aspire to register resources... (${waited}s elapsed)"
            waited=$((waited + 5))
            sleep 5
            continue
        fi

        echo "  Resources: ${healthy}/${total} healthy, ${pending} pending, ${failed} failed (${waited}s elapsed)"

        if [ "$failed" -gt 0 ]; then
            echo "ERROR: $failed resource(s) failed to start. Check Aspire dashboard for details."
            print_resource_table "$response"
            return 1
        fi

        if [ "$pending" -eq 0 ] && [ "$total" -gt 0 ]; then
            echo "All $total Aspire resources are healthy!"
            print_resource_table "$response"
            return 0
        fi

        waited=$((waited + 5))
        sleep 5
    done

    echo "WARN: Timed out after ${max_wait}s waiting for resources. Some may still be starting."
    if [ -n "$response" ]; then
        print_resource_table "$response"
    fi
    return 0  # Non-fatal: container keeps starting even if resources are slow
}

# ---------------------------------------------------------------------------
# Wait for PostgreSQL to be ready
# ---------------------------------------------------------------------------
echo "Waiting for PostgreSQL..."
pg_timeout=30
pg_counter=0
until PGPASSWORD="${POSTGRES_PASSWORD}" psql -h postgres -U "${POSTGRES_USER:-postgres}" -c '\q' 2>/dev/null; do
    pg_counter=$((pg_counter + 1))
    if [ $pg_counter -gt $pg_timeout ]; then
        echo "PostgreSQL not available after ${pg_timeout}s, continuing anyway..."
        break
    fi
    echo "Waiting for PostgreSQL... ($pg_counter/$pg_timeout)"
    sleep 1
done
if [ $pg_counter -le $pg_timeout ]; then
    echo "PostgreSQL is ready!"
fi

# ---------------------------------------------------------------------------
# Ensure Dapr is available
# ---------------------------------------------------------------------------
echo "Checking Dapr status..."
dapr --version || echo "Dapr CLI not found; skipping Dapr check"

# ---------------------------------------------------------------------------
# Start .NET Aspire via 'aspire run --detach'
#
# --detach  : parent process exits immediately after printing a JSON summary;
#             the AppHost continues running as a background child process.
# --format json : machine-readable output including dashboardUrl with ?t=TOKEN
# --non-interactive : suppress all prompts and spinners
#
# If already running (idempotent re-start), skip and recover PID + URL from
# the existing log file.
# ---------------------------------------------------------------------------
aspire_pid=""
dashboard_login_url=""
dashboard_base_url="https://localhost:15001"

if pgrep -f "MfePortal.AppHost" > /dev/null 2>&1; then
    aspire_pid=$(pgrep -f "MfePortal.AppHost" | head -1)
    echo "Aspire AppHost already running (PID $aspire_pid). Skipping start."
    # Best-effort: recover login URL from previous detach log
    if [ -f "$ASPIRE_LOG" ]; then
        dashboard_login_url=$(python3 -c "
import json, sys
try:
    data = json.load(open('$ASPIRE_LOG'))
    print(data.get('dashboardUrl', ''))
except Exception:
    pass
" 2>/dev/null)
    fi
else
    echo "Starting .NET Aspire AppHost (detached)..."

    if ! command -v aspire >/dev/null 2>&1; then
        echo "ERROR: 'aspire' CLI not found. Run: dotnet tool install --global Aspire.Cli"
        echo "  Then re-run this script."
        exit 1
    fi

    # Run detached; capture JSON output to log file and stdout
    aspire_json=$(aspire run \
        --non-interactive \
        --detach \
        --format json \
        --apphost /workspace/backend/MfePortal.AppHost \
        2>/dev/null) || true

    # Persist the JSON for idempotent re-starts
    echo "$aspire_json" > "$ASPIRE_LOG"

    # Parse PID and dashboard URL from JSON output
    aspire_pid=$(echo "$aspire_json" | python3 -c "
import json, sys
try:
    data = json.loads(sys.stdin.read())
    print(data.get('appHostPid', ''))
except Exception:
    pass
" 2>/dev/null)

    dashboard_login_url=$(echo "$aspire_json" | python3 -c "
import json, sys
try:
    data = json.loads(sys.stdin.read())
    print(data.get('dashboardUrl', ''))
except Exception:
    pass
" 2>/dev/null)

    if [ -z "$aspire_pid" ]; then
        echo "WARN: Could not determine AppHost PID from aspire run output."
        echo "  aspire output: $aspire_json"
    else
        echo "Aspire AppHost started (PID $aspire_pid)."
    fi
fi

# Derive base URL (strip /login?t=...) for the resource health poll endpoint
if [ -n "$dashboard_login_url" ]; then
    dashboard_base_url=$(echo "$dashboard_login_url" | python3 -c "
import sys
from urllib.parse import urlparse
u = urlparse(sys.stdin.read().strip())
print(u.scheme + '://' + u.netloc)
" 2>/dev/null)
fi

# ---------------------------------------------------------------------------
# Wait for all Aspire resources to become healthy
# ---------------------------------------------------------------------------
if command -v curl >/dev/null 2>&1; then
    if ! wait_for_aspire_resources "${dashboard_base_url}" 300 "${aspire_pid}"; then
        echo "WARN: Some Aspire resources failed to start. Check the dashboard for details."
    fi
else
    echo "curl not found; skipping resource health check."
fi

# ---------------------------------------------------------------------------
# Print the portal access URL for the host user
# ---------------------------------------------------------------------------
echo ""
echo "===================================================================="
echo "  Aspire Dashboard (accessible from host machine via VS Code):"
if [ -n "$dashboard_login_url" ]; then
    echo "  $dashboard_login_url"
else
    echo "  $dashboard_base_url"
    echo "  (Login URL not available — check $ASPIRE_LOG)"
fi
echo ""
echo "  Port 15001 is forwarded by VS Code Dev Containers."
echo "  Open the URL above in your host machine browser."
echo "===================================================================="
echo ""
echo "Post-start setup complete!"
echo ""
echo "Ready to develop! Available commands:"
echo "  cat $ASPIRE_LOG                                  (Aspire startup JSON / logs)"
echo "  cd frontend/shell && npm start                   (Start frontend dev server)"
echo "  dapr run --help                                  (Dapr sidecar commands)"
echo ""
