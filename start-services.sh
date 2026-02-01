#!/bin/bash
# Start Services Script for MFE Portal
# Run this inside the devcontainer to start both backend and frontend

set -e

echo "🚀 Starting MFE Portal Services..."
echo ""

# Check if we're in a container
if [ ! -f "/.dockerenv" ] && [ -z "$DOTNET_RUNNING_IN_CONTAINER" ]; then
    echo "⚠️  Warning: Not running in devcontainer. Consider opening in devcontainer first."
    echo "   Press Ctrl+C to cancel, or wait 5 seconds to continue anyway..."
    sleep 5
fi

# Function to start backend
start_backend() {
    echo "📦 Starting Backend (Aspire AppHost)..."
    cd /workspace/backend
    dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
}

# Function to start frontend
start_frontend() {
    echo "🎨 Starting Frontend (Piral Shell)..."
    sleep 10  # Give backend time to start
    cd /workspace/frontend/shell
    npm run start:1234
}

# Start backend in background
start_backend &
BACKEND_PID=$!

# Start frontend in background
start_frontend &
FRONTEND_PID=$!

echo ""
echo "✅ Services started!"
echo ""
echo "📊 Aspire Dashboard: https://localhost:15001"
echo "🖥️  Frontend: http://localhost:1234"
echo ""
echo "Backend PID: $BACKEND_PID"
echo "Frontend PID: $FRONTEND_PID"
echo ""
echo "Press Ctrl+C to stop all services..."

# Wait for both processes
wait $BACKEND_PID $FRONTEND_PID
