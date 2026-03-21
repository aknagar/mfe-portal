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

# Set git safe directory
git config --global --add safe.directory /workspace

echo "Post-create setup complete!"
echo ""
echo "Quick Start Commands:"
echo "  Backend:  cd backend && dotnet run --project MfePortal.AppHost"
echo "  Frontend: cd frontend/shell && npm start"
echo ""
