#!/bin/bash
set -e

echo "Running post-start setup..."

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

# Start .NET Aspire orchestrator in the background
echo "Starting .NET Aspire orchestrator..."
nohup dotnet run --project /workspace/backend/MfePortal.AppHost \
    --launch-profile http \
    > /tmp/aspire.log 2>&1 &
aspire_pid=$!

# Basic check: ensure the orchestrator process started
if ! kill -0 "$aspire_pid" 2>/dev/null; then
    echo "ERROR: Failed to start .NET Aspire orchestrator. See /tmp/aspire.log for details."
    exit 1
fi

echo "Aspire starting in background (PID $aspire_pid). Logs: /tmp/aspire.log"

# Optional readiness check for the Aspire dashboard
dashboard_url="${ASPIRE_DASHBOARD_URL:-http://localhost:15001}"
max_wait_seconds=30
waited=0

if command -v curl >/dev/null 2>&1; then
    echo "Waiting for Aspire dashboard to become available at ${dashboard_url} (up to ${max_wait_seconds}s)..."
    while [ $waited -lt $max_wait_seconds ]; do
        # Fail fast if the process died during startup
        if ! kill -0 "$aspire_pid" 2>/dev/null; then
            echo "ERROR: Aspire orchestrator process (PID $aspire_pid) exited during startup. See /tmp/aspire.log for details."
            exit 1
        fi

        if curl -fsS "${dashboard_url}" >/dev/null 2>&1; then
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
    echo "Aspire orchestrator is running with PID $aspire_pid. Check /tmp/aspire.log for readiness details."
fi

echo "Dashboard will be available at ${dashboard_url} once ready."

echo "Post-start setup complete!"
echo ""
echo "Ready to develop! Available commands:"
echo "  tail -f /tmp/aspire.log                          (Follow Aspire logs)"
echo "  cd frontend/shell && npm start                   (Start frontend dev server)"
echo "  dapr run --help                                  (Dapr sidecar commands)"
echo ""
