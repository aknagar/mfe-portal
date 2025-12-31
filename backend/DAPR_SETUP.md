# Dapr Integration with .NET Aspire

This guide explains how to integrate Dapr (Distributed Application Runtime) with .NET Aspire for local development and production deployments.

## Overview

Dapr provides distributed application capabilities:
- **State Management**: Persist and retrieve application state
- **Pub/Sub Messaging**: Asynchronous event-driven communication
- **Service Invocation**: Direct service-to-service communication
- **Bindings**: Connect to external systems and resources

## Architecture

Dapr runs as a **sidecar process** alongside your application:

```
┌─────────────────────────────────────┐
│  .NET Aspire (Process-based)        │
│  - MfePortal.AppHost                │
│  - AugmentService                   │
└────────────┬────────────────────────┘
             │
        ┌────┴──────────────┐
        │                   │
    ┌───────┐          ┌──────────────┐
    │ Redis │          │ Dapr Sidecar │
    │ Store │◄────────►│  (HTTP/gRPC) │
    └───────┘          └──────────────┘
```

## Prerequisites

1. **Dapr CLI**: Install from [dapr.io](https://dapr.io/download)
2. **Redis**: For state store and pub/sub
3. **.NET 9.0 SDK**
4. **Docker** (for running Redis)

## Local Development Setup

### Step 1: Start Redis

```bash
# Using Docker
docker run -d --name dapr-redis -p 6379:6379 redis:7-alpine

# Or using WSL2 with Docker Desktop on Windows
docker run -d --name dapr-redis -p 6379:6379 redis:7-alpine
```

Verify Redis is running:
```bash
redis-cli ping
# Response: PONG
```

### Step 2: Initialize Dapr (First Time Only)

```bash
dapr init --slim
```

The `--slim` flag initializes Dapr without Docker containers, using local binaries.

### Step 3: Run AugmentService with Dapr Sidecar

In a new terminal:

```bash
cd backend/AugmentService

# Run with Dapr sidecar
dapr run --app-id augmentservice \
  --app-port 7139 \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --components-path ../dapr/components \
  dotnet run
```

### Step 4: Run Aspire AppHost

In another terminal:

```bash
cd backend

# Set Dapr environment variables for child processes
$env:DAPR_HTTP_ENDPOINT = "http://localhost:3500"

dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
```

## Using Dapr in AugmentService

### Inject DaprClient

Register Dapr support in `Program.cs`:

```csharp
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

// Register Dapr client
builder.Services.AddDaprClient();

var app = builder.Build();

// Use in endpoints
app.MapPost("/save-state", async (DaprClient daprClient, string key, object value) =>
{
    await daprClient.SaveStateAsync("statestore", key, value);
    return Results.Ok("State saved");
})
.WithName("SaveState")
.WithOpenApi();

app.MapGet("/get-state/{key}", async (DaprClient daprClient, string key) =>
{
    var state = await daprClient.GetStateAsync<object>("statestore", key);
    return state is not null ? Results.Ok(state) : Results.NotFound();
})
.WithName("GetState")
.WithOpenApi();

app.Run();
```

### State Management Examples

**Save application state**:
```csharp
await daprClient.SaveStateAsync("statestore", "user-123", new { Name = "John", Email = "john@example.com" });
```

**Retrieve state**:
```csharp
var user = await daprClient.GetStateAsync<User>("statestore", "user-123");
```

**Delete state**:
```csharp
await daprClient.DeleteStateAsync("statestore", "user-123");
```

### Pub/Sub Messaging Examples

**Publish event**:
```csharp
await daprClient.PublishEventAsync("pubsub", "orders", new { OrderId = 123, Amount = 99.99 });
```

**Subscribe to topic** (in a separate service or listener):
```csharp
app.MapPost("/orders", async (OrderEvent order) =>
{
    // Process order event
    return Results.Ok();
})
.WithName("ProcessOrder")
.WithTopic("pubsub", "orders");
```

## Component Configuration

### State Store (statestore.yaml)
Configured for Redis-backed state management:
```yaml
metadata:
  - name: redisHost
    value: localhost:6379
  - name: actorStateStore
    value: "true"
```

### Pub/Sub (pubsub.yaml)
Configured for Redis-backed pub/sub messaging:
```yaml
metadata:
  - name: redisHost
    value: localhost:6379
```

## Troubleshooting

### Dapr Sidecar Won't Start

```bash
# Check Dapr version
dapr --version

# Check if Redis is accessible
redis-cli ping

# Try explicit Dapr init
dapr init --runtime-version latest
```

### Connection Refused (localhost:3500)

Ensure:
1. Dapr sidecar started with correct port: `--dapr-http-port 3500`
2. DaprClient configured with matching endpoint:
   ```csharp
   builder.Services.AddDaprClient(client => 
   {
       client.UseJsonSerializationOptions(new JsonSerializerOptions(...));
   });
   ```

### State Not Persisting

1. Verify Redis is running: `redis-cli ping`
2. Check component configuration points to correct Redis host
3. Review Dapr logs: `dapr logs`

## Environment Variables

Set these in your shell before running services:

```bash
# HTTP endpoint for Dapr sidecar
$env:DAPR_HTTP_ENDPOINT = "http://localhost:3500"

# gRPC endpoint (used by Dapr internally)
$env:DAPR_GRPC_ENDPOINT = "http://localhost:50001"

# Application port
$env:ASPNETCORE_URLS = "https://localhost:7139"
```

## Production Deployment

For production with Kubernetes:

1. **Deploy Dapr to Kubernetes**:
   ```bash
   dapr init -k
   ```

2. **Annotate Kubernetes services** for sidecar injection:
   ```yaml
   metadata:
     annotations:
       dapr.io/enabled: "true"
       dapr.io/app-id: "augmentservice"
       dapr.io/app-port: "8080"
   ```

3. **Use managed state stores**:
   - Azure Cosmos DB
   - Azure Service Bus
   - AWS DynamoDB
   - Google Cloud Datastore

4. **Deploy components** as Kubernetes resources:
   ```bash
   kubectl apply -f dapr/components/
   ```

## Development vs Production

| Aspect | Development | Production |
|--------|-------------|-----------|
| **Runtime** | Dapr CLI (local) | Dapr on Kubernetes |
| **State Store** | Local Redis | Managed cloud service |
| **Pub/Sub** | Local Redis | Cloud message broker |
| **Configuration** | YAML files | Kubernetes resources |
| **Networking** | localhost | Service discovery |

## Stopping Services

```bash
# Stop Dapr sidecar
dapr stop augmentservice

# Stop Redis
docker stop dapr-redis
docker rm dapr-redis

# Cleanup Dapr
dapr uninstall --all
```

## References

- [Dapr Documentation](https://docs.dapr.io/)
- [Dapr .NET SDK](https://github.com/dapr/dotnet-sdk)
- [Dapr Components](https://docs.dapr.io/reference/components-reference/)
- [Aspire Orchestration](https://learn.microsoft.com/en-us/dotnet/aspire/)
