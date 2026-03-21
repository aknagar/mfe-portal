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

# Configure Docker daemon for host.docker.internal resolution on Linux
# (Docker Desktop handles this automatically on macOS/Windows; Linux needs explicit config)
echo "Configuring Docker daemon for host.docker.internal resolution on Linux..."
HOST_IP=$(ip route show default 2>/dev/null | awk 'NR==1 && /via/ { print $3 }')
if [ -n "$HOST_IP" ] && [[ "$HOST_IP" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    mkdir -p /etc/docker
    if [ -f /etc/docker/daemon.json ]; then
        tmp=$(mktemp)
        jq --arg ip "${HOST_IP}" '. + {"host-gateway-ip": $ip}' /etc/docker/daemon.json > "$tmp" \
            && mv "$tmp" /etc/docker/daemon.json \
            || echo "Warning: could not merge daemon.json; skipping host-gateway-ip config"
    else
        printf '{\n  "host-gateway-ip": "%s"\n}\n' "${HOST_IP}" > /etc/docker/daemon.json
    fi
    echo "Restarting Docker daemon to apply host-gateway-ip=${HOST_IP}..."
    if ! service docker restart 2>/dev/null; then
        echo "Warning: 'service docker restart' failed; daemon.json configuration may not have been applied."
    fi
    DOCKER_READY=0
    for i in $(seq 1 15); do
        docker info >/dev/null 2>&1 && DOCKER_READY=1 && break
        echo "Waiting for Docker daemon... ($i/15)"
        sleep 1
    done
    if [ "$DOCKER_READY" -eq 1 ]; then
        echo "Docker daemon configured: host.docker.internal will resolve to ${HOST_IP} in all containers."
    else
        echo "Warning: Docker daemon did not become ready within 15 seconds; subsequent steps may fail."
    fi
else
    echo "Warning: could not determine valid host gateway IP (got: '${HOST_IP:-empty}'); host.docker.internal may not resolve in Aspire-managed containers."
fi

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
echo "Pre-pulling container images required by Aspire (first run may take several minutes)..."
docker pull mcr.microsoft.com/azure-messaging/servicebus-emulator:latest || echo "Warning: failed to pull servicebus-emulator:latest"
docker pull mcr.microsoft.com/azure-messaging/servicebus-emulator:1.1.2 || echo "Warning: failed to pull servicebus-emulator:1.1.2"
docker pull mcr.microsoft.com/mssql/server:2022-latest || echo "Warning: failed to pull mssql/server:2022-latest"
docker pull ghcr.io/diagridio/diagrid-dashboard:0.0.1 || echo "Warning: failed to pull diagrid-dashboard:0.0.1"
echo "Image pre-pull complete (check above for any warnings)."

# Set git safe directory
git config --global --add safe.directory /workspace

echo "Post-create setup complete!"
echo ""
echo "Quick Start Commands:"
echo "  Backend:  cd backend && dotnet run --project MfePortal.AppHost"
echo "  Frontend: cd frontend/shell && npm start"
echo ""
