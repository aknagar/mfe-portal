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

The Dapr sidecar is managed automatically by .NET Aspire. There is no need to run `dapr run`
manually — Aspire's `WithDaprSidecar()` starts and configures the sidecar for `augmentservice`
using the component templates in `backend/MfePortal.AppHost/.dapr/components/`.

### Step 1: Install the Dapr CLI (First Time Only)

The Aspire toolkit invokes the `dapr` binary to start the sidecar. Install it once:

```bash
# Windows (winget)
winget install Dapr.CLI

# macOS
brew install dapr/tap/dapr

# Verify
dapr --version
```

### Step 2: Initialize Dapr (First Time Only)

```bash
dapr init --slim
```

The `--slim` flag initializes Dapr without Docker containers, using local binaries.

### Step 3: Run the Full Stack via Aspire AppHost

```bash
cd backend
dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
```

Aspire starts Redis, the Dapr sidecar for `augmentservice`, and the service itself.
The sidecar is configured from the component templates in
`backend/MfePortal.AppHost/.dapr/components/` with Redis connection details injected
automatically by Aspire's `WithMetadata()` calls.

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

Component templates live in `backend/MfePortal.AppHost/.dapr/components/`. Aspire reads these
at startup, injects the Redis connection details (host, port, password) from `WithMetadata()`,
and writes the final YAML to a temp directory passed to the Dapr CLI.

> **Naming rule**: Files must be named after the Dapr component **type** (e.g. `state.yaml`,
> `pubsub.yaml`), not the resource name. The toolkit probes by type when no `LocalPath` is set.

### State Store (`state.yaml`)
Redis-backed state store with `actorStateStore: "true"` (required by Dapr Workflow):
```yaml
spec:
  type: state.redis
  metadata:
  - name: redisHost
    secretKeyRef:
      name: STATESTORE_REDISHOST
  - name: actorStateStore
    value: "true"
```

### Pub/Sub (`pubsub.yaml`)
Redis-backed pub/sub:
```yaml
spec:
  type: pubsub.redis
  metadata:
  - name: redisHost
    secretKeyRef:
      name: PUBSUB_REDISHOST
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
    kubectl apply -f MfePortal.AppHost/.dapr/components/
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
