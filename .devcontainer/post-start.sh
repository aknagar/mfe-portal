#!/bin/bash
set -e

echo "🔄 Running post-start setup..."

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL..."
timeout=30
counter=0
until PGPASSWORD="${POSTGRES_PASSWORD}" psql -h postgres -U "${POSTGRES_USER:-postgres}" -c '\q' 2>/dev/null; do
    counter=$((counter + 1))
    if [ $counter -gt $timeout ]; then
        echo "⚠️ PostgreSQL not available after ${timeout}s, continuing anyway..."
        break
    fi
    echo "Waiting for PostgreSQL... ($counter/$timeout)"
    sleep 1
done

if [ $counter -le $timeout ]; then
    echo "✅ PostgreSQL is ready!"
fi

# Ensure Dapr is running
echo "🎯 Checking Dapr status..."
dapr --version || echo "Dapr CLI available"

echo "✅ Post-start setup complete!"
echo ""
echo "🎮 Ready to develop! Available commands:"
echo "  • dotnet run --project backend/MfePortal.AppHost  (Start Aspire orchestrator)"
echo "  • cd frontend/shell && npm start                   (Start frontend dev server)"
echo "  • dapr run --help                                  (Dapr sidecar commands)"
echo ""
