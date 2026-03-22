#!/bin/bash
set -e

readonly ASPIRE_LOG="/tmp/aspire.log"

echo "Running post-start setup..."

# Extract Aspire dashboard login token from the log
get_aspire_token() {
    local log_file="$ASPIRE_LOG"
    local token=""
    # The token appears as: Login to the dashboard at https://...?t=<TOKEN>
    # Requires GNU grep (available on ubuntu-22.04 devcontainer base)
    token=$(grep -oP '(?<=\?t=)[A-Za-z0-9_-]+' "$log_file" 2>/dev/null | tail -1)
    echo "$token"
}

# Print a formatted table of Aspire resource states
print_resource_table() {
    local response="$1"
    echo ""
    echo "+--------------------------------------------------------------+"
    echo "|              Aspire Resource Status                          |"
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
        [ -z "$name" ] && continue
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

# Poll Aspire Resource Service API until all resources are healthy
wait_for_aspire_resources() {
    local dashboard_url="${1:-https://localhost:15001}"
    local token="${2:-}"
    local max_wait="${3:-300}"
    local pid="${4:-$aspire_pid}"
    local waited=0
    local auth_header=""

    if [ -n "$token" ]; then
        auth_header="Authorization: Bearer $token"
    fi

    echo ""
    echo "Waiting for all Aspire resources to become healthy (up to ${max_wait}s)..."

    local response=""

    while [ $waited -lt $max_wait ]; do
        # Check if Aspire process is still alive
        if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then
            echo "ERROR: Aspire orchestrator exited during resource startup. See $ASPIRE_LOG"
            return 1
        fi

        # Query resource list
        if [ -n "$auth_header" ]; then
            response=$(curl -fsSk -H "$auth_header" "${dashboard_url}/api/v1/resources" 2>/dev/null)
        else
            response=$(curl -fsSk "${dashboard_url}/api/v1/resources" 2>/dev/null)
        fi

        if [ -z "$response" ]; then
            waited=$((waited + 5))
            sleep 5
            continue
        fi

        # Count resources still in transitional states
        # Requires GNU grep (available on ubuntu-22.04 devcontainer base)
        local pending
        pending=$(echo "$response" | grep -oP '"state"\s*:\s*"(Starting|Building|Waiting|NotStarted)"' | wc -l)
        local failed
        failed=$(echo "$response" | grep -oP '"state"\s*:\s*"(FailedToStart|RuntimeUnhealthy)"' | wc -l)
        local healthy
        healthy=$(echo "$response" | grep -oP '"state"\s*:\s*"(Running|Finished|Exited)"' | wc -l)
        local total
        total=$(( healthy + pending + failed ))

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

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL..."
timeout=30
counter=0
until PGPASSWORD="${POSTGRES_PASSWORD}" psql -h postgres -U "${POSTGRES_USER:-postgres}" -c '\q' 2>/dev/null; do
    counter=$((counter + 1))
    if [ $counter -gt $timeout ]; then
        echo "PostgreSQL not available after ${timeout}s, continuing anyway..."
        break
    fi
    echo "Waiting for PostgreSQL... ($counter/$timeout)"
    sleep 1
done

if [ $counter -le $timeout ]; then
    echo "PostgreSQL is ready!"
fi

# Ensure Dapr is running
echo "Checking Dapr status..."
dapr --version || echo "Dapr CLI available"

# Start .NET Aspire orchestrator in the background (idempotent — skip if already running)
if pgrep -f "MfePortal.AppHost" > /dev/null 2>&1; then
    aspire_pid=$(pgrep -f "MfePortal.AppHost" | head -1)
    echo "Aspire orchestrator already running (PID $aspire_pid). Skipping start."
else
    echo "Starting .NET Aspire orchestrator..."
    nohup dotnet run --project /workspace/backend/MfePortal.AppHost \
        --launch-profile https \
        > "$ASPIRE_LOG" 2>&1 &
    aspire_pid=$!

    # Basic check: ensure the orchestrator process started
    if ! kill -0 "$aspire_pid" 2>/dev/null; then
        echo "ERROR: Failed to start .NET Aspire orchestrator. See $ASPIRE_LOG for details."
        exit 1
    fi

    echo "Aspire starting in background (PID $aspire_pid). Logs: $ASPIRE_LOG"
fi

# Optional readiness check for the Aspire dashboard
dashboard_url="${ASPIRE_DASHBOARD_URL:-https://localhost:15001}"
max_wait_seconds=120
waited=0

if command -v curl >/dev/null 2>&1; then
    echo "Waiting for Aspire dashboard to become available at ${dashboard_url} (up to ${max_wait_seconds}s)..."
    while [ $waited -lt $max_wait_seconds ]; do
        # Fail fast if the process died during startup
        if ! kill -0 "$aspire_pid" 2>/dev/null; then
            echo "ERROR: Aspire orchestrator process (PID $aspire_pid) exited during startup. See $ASPIRE_LOG for details."
            exit 1
        fi

        if curl -fsSk "${dashboard_url}" >/dev/null 2>&1; then
            echo "Aspire dashboard is ready at ${dashboard_url}."
            break
        fi

        waited=$((waited + 1))
        sleep 1
    done

    if [ $waited -ge $max_wait_seconds ]; then
        echo "Aspire dashboard did not become reachable within ${max_wait_seconds}s. It may still be starting."
    fi
else
    echo "curl not found; skipping HTTP health check for Aspire dashboard."
    echo "Aspire orchestrator is running with PID $aspire_pid. Check $ASPIRE_LOG for readiness details."
fi

echo "Dashboard will be available at ${dashboard_url} once ready."

echo "Post-start setup complete!"
echo ""
echo "Ready to develop! Available commands:"
echo "  tail -f $ASPIRE_LOG                          (Follow Aspire logs)"
echo "  cd frontend/shell && npm start                   (Start frontend dev server)"
echo "  dapr run --help                                  (Dapr sidecar commands)"
echo ""
