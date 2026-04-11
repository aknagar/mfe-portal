# Local Testing Guide

This guide explains how to run and test the MfePortal solution locally with Dapr integration.

## Quick Start (Without Dapr)

### 1. Start AugmentService

From the `backend/AugmentService` directory:

```bash
dotnet run
```

The service will start on `https://localhost:7139`

### 2. Run Tests

Open another terminal and run:

```bash
cd backend
pwsh ./test-local.ps1
```

This script tests:
- ✓ Service health check
- ✓ /health endpoint
- ✓ /alive endpoint
- ✓ /swagger endpoint
- ✓ /proxy endpoint
- ✓ /openapi/v1.json endpoint

## Complete Setup (With Dapr)

### Prerequisites

```bash
# Install Dapr CLI (Windows)
winget install Dapr.CLI

# Or on macOS
brew install dapr/tap/dapr

# Verify Dapr installation
dapr --version
```

### Step 1: Initialize Dapr (First Time Only)

```bash
dapr init --slim
```

The `--slim` flag uses local binaries instead of Docker containers.

### Step 2: Run the Full Stack via Aspire

Aspire starts Redis, the Dapr sidecar, and `augmentservice` in one command:

```bash
cd backend
dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
```

Expected output (Aspire dashboard will open in browser):
```
Starting Aspire AppHost...
Redis container started
Dapr sidecar started for augmentservice (HTTP: 3500, gRPC: 50001)
augmentservice started
```

### Step 3: Test with Dapr

Run the test script:

```bash
cd backend
pwsh ./test-local.ps1
```

## Testing Endpoints Manually

### Using PowerShell

```powershell
# Skip certificate warnings for localhost
$PSDefaultParameterValues["Invoke-WebRequest:SkipCertificateCheck"] = $true

# Test health
Invoke-WebRequest -Uri "https://localhost:7139/health" -Method GET

# Test liveness
Invoke-WebRequest -Uri "https://localhost:7139/alive" -Method GET

# Test proxy
Invoke-WebRequest -Uri "https://localhost:7139/proxy?url=https://httpbin.org/status/200" -Method GET

# Test Swagger
Start-Process "https://localhost:7139/swagger"
```

### Using curl (Git Bash / WSL)

```bash
# Test health
curl -k https://localhost:7139/health

# Test liveness  
curl -k https://localhost:7139/alive

# Test proxy
curl -k "https://localhost:7139/proxy?url=https://httpbin.org/status/200"

# Get OpenAPI spec
curl -k https://localhost:7139/openapi/v1.json | jq .
```

### Using Dapr CLI

```bash
# Invoke proxy endpoint via Dapr service invocation
dapr invoke --app-id augmentservice \
  --method /proxy \
  --verb GET \
  -d '{"url":"https://httpbin.org/status/200"}'

# Get state
dapr state get --state-store statestore statekey

# Publish event
dapr publish --publish-appid augmentservice \
  --pubsub pubsub \
  --topic orders \
  --data '{"orderId":123}'
```

## Dapr Features to Test

### State Management

Once AugmentService is running with Dapr, you can test state persistence:

```bash
# Access Redis state directly
redis-cli GET "statekey"

# Via Dapr HTTP API
curl -X POST http://localhost:3500/v1.0/state/statestore \
  -H "Content-Type: application/json" \
  -d '[{"key":"testkey","value":"testvalue"}]'

curl http://localhost:3500/v1.0/state/statestore/testkey
```

### Pub/Sub Messaging

```bash
# Publish to topic via Dapr
curl -X POST http://localhost:3500/v1.0/publish/pubsub/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId":123,"amount":99.99}'
```

## Troubleshooting

### Redis Not Connecting

**Issue**: Dapr fails to connect to Redis
```
error connecting to redis at localhost:6379
```

**Solution**:
```bash
# Check Redis is running
docker ps | grep dapr-redis

# Verify Redis port
redis-cli ping

# Check firewall rules
```

### Dapr Port Already in Use

**Issue**: Port 3500 or 50001 already in use

**Solution**:
```bash
# Find process using port 3500
netstat -ano | findstr :3500

# Kill the process
taskkill /PID <PID> /F
```

### Certificate Validation Errors

**Issue**: HTTPS certificate validation errors

**Solution**: Use `-SkipCertificateCheck` for localhost testing:
```powershell
$PSDefaultParameterValues["Invoke-WebRequest:SkipCertificateCheck"] = $true
```

### Service Not Responding

**Issue**: Cannot connect to service

**Steps**:
1. Check service is running: `netstat -ano | findstr :7139`
2. Check logs for errors in the service console
3. Verify HTTPS configuration in `launchSettings.json`
4. Try accessing without Dapr first: `dotnet run` without dapr wrapper

## Performance Testing

### Load Testing with ApacheBench

```bash
# Test health endpoint with 1000 requests, 10 concurrent
ab -n 1000 -c 10 -k https://localhost:7139/health

# Test proxy endpoint
ab -n 100 -c 5 -k "https://localhost:7139/proxy?url=https://httpbin.org/status/200"
```

### Using k6

```javascript
// save as test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '2m', target: 100 },
    { duration: '5m', target: 100 },
    { duration: '2m', target: 0 },
  ],
};

export default function () {
  const response = http.get('https://localhost:7139/health');
  check(response, {
    'health check passed': (r) => r.status === 200,
  });
  sleep(1);
}
```

```bash
k6 run test.js --insecure-skip-tls-verify
```

## Cleanup

### Stop Services

```bash
# Stop AugmentService (Ctrl+C in the terminal)

# Stop Dapr sidecar
dapr stop augmentservice

# Stop Redis
docker stop dapr-redis
docker rm dapr-redis

# Cleanup Dapr
dapr uninstall --all
```

### Clean Build Artifacts

```bash
cd backend
dotnet clean
rm -r AugmentService/bin
rm -r AugmentService/obj
```

## Next Steps

1. **Add Dapr State Endpoints**: Extend AugmentService to use DaprClient for state management
2. **Pub/Sub Integration**: Add message handlers for Dapr topics
3. **Service Invocation**: Test service-to-service communication
4. **Docker Deployment**: Build and test containerized version
5. **Azure Deployment**: Use the infrastructure in `infra/` to deploy to Azure

## References

- [AugmentService API Documentation](./AugmentService/API_DOCUMENTATION.md)
- [Dapr Local Development](../DAPR_SETUP.md)
- [Azure Deployment](./infra/DEPLOYMENT.md)
- [Dapr Documentation](https://docs.dapr.io/)
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Load Testing](https://www.mostlylucid.net/blog/k6-testing-practical)