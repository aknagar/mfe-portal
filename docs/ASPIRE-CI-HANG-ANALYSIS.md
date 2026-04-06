# Aspire Deploy CI Hang — Root Cause Analysis

## Problem

`aspire deploy` hangs indefinitely at the `process-parameters` pipeline step in CI (GitHub Actions, no TTY). Every run silently stalls for ~23 minutes until GitHub cancels the job at the configured timeout.

## Symptoms

- CI log shows `process-parameters` step starting and then nothing.
- No error, no output — just silence until the runner is killed.
- Affects both Test and Production deploy workflows.
- Reproducible on every run.

---

## Root Cause (Confirmed)

### The hang chain — step by step

```
aspire deploy
  └─ launches AppHost with --publisher AzureContainerAppEnvironmentPublisher
       └─ DistributedApplicationPipeline runs "process-parameters" step
            └─ ParameterProcessor.InitializeParametersAsync
                 └─ CollectDependentParameterResourcesAsync
                      └─ iterates every resource, calls GetResourceDependenciesAsync
                           └─ GatherRawEnvironmentAndArgumentValuesAsync
                                └─ invokes every EnvironmentCallbackAnnotation on DaprSidecarResource
                                     └─ DaprDistributedApplicationLifecycleHook added EnvironmentCallbackAnnotation
                                          └─ calls valueProvider.GetValueAsync(cancellationToken)
                                               └─ valueProvider = ReferenceExpression wrapping
                                                  BicepOutputReference("hostName", daprRedis.Resource)
                                                    └─ BicepOutputReference.GetValueAsync:
                                                         await Resource.ProvisioningTaskCompletionSource.Task
                                                              └─ HANG — TCS only completed by "provision-daprRedis"
                                                                         step which runs AFTER process-parameters
```

### The Dapr `WithMetadata(IValueProvider)` callback — exact mechanics

`DaprMetadataResourceBuilderExtensions.WithMetadata(IValueProvider)` adds a
`DaprComponentValueProviderAnnotation` to the `DaprComponentResource` (pubsub).

`DaprDistributedApplicationLifecycleHook.OnBeforeStartAsync` then reads those annotations from each
component referenced by a sidecar, and adds a single **unconditional**
`EnvironmentCallbackAnnotation` to the `DaprSidecarResource`:

```csharp
// No publish-mode guard here:
daprSidecar.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
{
    foreach (var (envVarName, valueProvider) in endpointEnvironmentVars)
    {
        var value = await valueProvider.GetValueAsync(context.CancellationToken); // ← BLOCKS
        context.EnvironmentVariables.TryAdd(envVarName, value ?? string.Empty);
    }
}));
```

### Why `BicepOutputReference.GetValueAsync` blocks in publish mode

```csharp
// Aspire.Hosting.Azure — BicepOutputReference.cs
public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
{
    TaskCompletionSource provisioningTaskCompletionSource = Resource.ProvisioningTaskCompletionSource;
    if (provisioningTaskCompletionSource != null)
    {
        // Waits for the "provision-daprRedis" pipeline step to complete.
        // In publish mode, that step runs AFTER "process-parameters".
        // → Deadlock.
        await provisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
    return Value;
}
```

The `ProvisioningTaskCompletionSource` is set for every Azure Bicep resource during pipeline
configuration. It is resolved when its `provision-<name>` step runs. That step is tagged
`provision-azure-bicep-resources` which runs **after** `process-parameters`. So inside
`process-parameters`, calling `GetValueAsync` on any `BicepOutputReference` deadlocks.

### The affected resources in `Program.cs`

```csharp
var daprRedis = builder.AddAzureManagedRedis("daprRedis");
var redisHost = daprRedis.Resource.HostName;    // ReferenceExpression → BicepOutputReference("hostName")
var redisPort = daprRedis.Resource.Port;         // ReferenceExpression → BicepOutputReference (hardcoded 10000)
var redisPassword = daprRedis.Resource.Password; // null in Azure Entra mode; ParameterResource in container mode

builder.AddDaprPubSub("pubsub")
    .WithMetadata("redisHost", ReferenceExpression.Create($"{redisHost}:{redisPort}"))  // ← IValueProvider path
    .WithMetadata("redisPassword", redisPassword);                                       // ← IValueProvider path
```

Both `redisHost` and `redisPort` are `ReferenceExpression`s whose `GetValueAsync` delegates to
`BicepOutputReference.GetValueAsync` → waits on provisioning TCS → **hang**.

---

## Why Earlier Mitigations Did Not Work

| Attempted fix | Why it failed |
|---|---|
| `DOTNET_ASPIRE_NONINTERACTIVE=true` | Controls CLI output/spinners only. Not read by the AppHost. |
| `--non-interactive` flag on `aspire deploy` | CLI-only. Does not set `ASPIRE_INTERACTIVITY_ENABLED` on the AppHost subprocess. |
| `ASPIRE_INTERACTIVITY_ENABLED=false` on deploy step | **Addresses a different bug** (interactive parameter prompts). Does NOT prevent the Dapr `WithMetadata` callback from blocking on `BicepOutputReference.GetValueAsync`. |
| `--parameter postgres-password=X` | The `--parameter` flag is not forwarded to the AppHost. |
| `timeout-minutes: 30` | A safety net, not a fix. |

---

## The Fix

**`backend/MfePortal.AppHost/Program.cs`** — skip `WithMetadata(IValueProvider)` calls in publish mode:

```csharp
var pubSubBuilder = builder.AddDaprPubSub("pubsub")
                    .WithMetadata("enableTLS", "true");

if (!builder.ExecutionContext.IsPublishMode)
{
    // Only inject BicepOutputReference-backed values at runtime.
    // In publish mode, GetValueAsync() on these would deadlock (see analysis above).
    // The Dapr sidecar CLI does not run in publish mode so these env vars are not needed.
    pubSubBuilder
        .WithMetadata("redisHost", ReferenceExpression.Create($"{redisHost}:{redisPort}"));

    if (redisPassword is not null)
    {
        pubSubBuilder.WithMetadata("redisPassword", redisPassword);
    }
}
```

### Why this is correct

- In **publish mode**, the Dapr sidecar CLI is excluded from the manifest
  (`ManifestPublishingCallbackAnnotation.Ignore` is on `daprCli`). The `EnvironmentCallbackAnnotation`
  that calls `GetValueAsync` is only needed for the live runtime sidecar process — not for
  manifest generation.
- In **run mode** (local development / `aspire run`), the guard is false, so the metadata
  is injected as before. Local Redis container values resolve synchronously (no TCS wait).
- `"enableTLS"` uses the `WithMetadata(string, string)` overload which never calls `GetValueAsync`.

---

## Aspire Source Files Consulted

| File | Purpose |
|---|---|
| `src/Aspire.Hosting/Pipelines/DistributedApplicationPipeline.cs` | `process-parameters` step definition |
| `src/Aspire.Hosting/Orchestrator/ParameterProcessor.cs` | `CollectDependentParameterResourcesAsync` |
| `src/Aspire.Hosting/ApplicationModel/ResourceExtensions.cs` | `GetResourceDependenciesAsync`, `GatherRawEnvironmentAndArgumentValuesAsync` |
| `src/Aspire.Hosting.Azure/Aspire.Hosting.Azure.dll` (decompiled) | `BicepOutputReference.GetValueAsync` — the blocking call |
| `src/Aspire.Hosting.Azure.Redis/Aspire.Hosting.Azure.Redis.dll` (decompiled) | `AzureManagedRedisResource.HostName/Port/Password` → `BicepOutputReference` |
| `CommunityToolkit.Aspire.Hosting.Dapr/DaprDistributedApplicationLifecycleHook.cs` | Unconditional `EnvironmentCallbackAnnotation` on sidecar |
| `CommunityToolkit.Aspire.Hosting.Dapr/DaprMetadataResourceBuilderExtensions.cs` | `WithMetadata(IValueProvider)` — adds `DaprComponentValueProviderAnnotation` |
| `CommunityToolkit.Aspire.Hosting.Dapr/DaprComponentValueProviderAnnotation.cs` | Annotation record |

---

## Commits

| Commit | Change |
|---|---|
| `84e1578` | Add `--non-interactive` and `Azure__` env vars to CD deploy steps |
| `9dc9dcb` | Upgrade Aspire CLI to 13.2.0, add `--non-interactive` to prod deploy |
| `fb44be6` | Add trace logging and exception details to CD test deploy |
| `f76e173` | Remove `--parameter` flags, add `timeout-minutes: 30`, enable debug logging |
| `d774e0b` | Set `ASPIRE_INTERACTIVITY_ENABLED=false` (addresses interactive-prompts bug, not this one) |
| `5102a75` | Add `docs/ASPIRE-CI-HANG-ANALYSIS.md` (initial draft — incorrect root cause) |
| *(next)* | **Skip `WithMetadata(IValueProvider)` in publish mode — the actual fix** |
