#!/bin/bash
set -e

echo "Running post-create setup..."

# Trust HTTPS development certificate
echo "Setting up HTTPS development certificates..."
dotnet dev-certs https --trust 2>/dev/null || true

# Restore backend packages
echo "Restoring backend NuGet packages..."
cd /workspace/backend
dotnet restore MfePortal.Backend.sln

# Install frontend dependencies
echo "Installing frontend npm packages..."
cd /workspace/frontend/shell
npm install

# Build frontend Docker image
echo "Building frontend Docker image..."
cd /workspace/frontend
docker build -t frontend:latest . || echo "Frontend Docker image build failed - ensure Docker daemon is running"

# Initialize Dapr
echo "Initializing Dapr..."
dapr init --slim || echo "Dapr initialization skipped (may already be initialized)"

# Add Dapr to PATH
echo "Configuring Dapr PATH..."
if ! grep -q ".dapr/bin" ~/.bashrc; then
    echo 'export PATH="$HOME/.dapr/bin:$PATH"' >> ~/.bashrc
fi
export PATH="$HOME/.dapr/bin:$PATH"

# Pre-pull Aspire-managed container images to avoid first-boot download races
echo "Pre-pulling container images required by Aspire (this may take a few minutes on first run)..."
docker pull mcr.microsoft.com/azure-messaging/servicebus-emulator:latest || echo "Warning: failed to pull servicebus-emulator:latest"
docker pull mcr.microsoft.com/azure-messaging/servicebus-emulator:1.1.2 || echo "Warning: failed to pull servicebus-emulator:1.1.2"
docker pull mcr.microsoft.com/mssql/server:2022-latest || echo "Warning: failed to pull mssql/server:2022-latest"
docker pull ghcr.io/diagridio/diagrid-dashboard:0.0.1 || echo "Warning: failed to pull diagrid-dashboard:0.0.1"
echo "Image pre-pull complete."

# Set git safe directory
git config --global --add safe.directory /workspace

echo "Post-create setup complete!"
echo ""
echo "Quick Start Commands:"
echo "  Backend:  cd backend && dotnet run --project MfePortal.AppHost"
echo "  Frontend: cd frontend/shell && npm start"
echo ""
