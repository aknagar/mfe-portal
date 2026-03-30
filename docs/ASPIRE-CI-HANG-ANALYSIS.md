# Aspire Deploy CI Hang — Root Cause Analysis

## Problem

`aspire deploy` hangs indefinitely at the `process-parameters` pipeline step in CI (GitHub Actions, no TTY). Every run silently stalls for ~23 minutes until GitHub cancels the job at the configured timeout.

## Symptoms

- CI log shows `process-parameters` step starting and then nothing.
- No error, no output — just silence until the runner is killed.
- Affects both Test and Production deploy workflows.
- Reproducible on every run.

---

## Root Cause

### The hang chain — step by step

```
aspire deploy
  └─ launches AppHost with --operation publish --step deploy
       └─ DistributedApplicationPipeline runs "process-parameters" step
            └─ ParameterProcessor.InitializeParametersAsync(waitForResolution: true)
                 └─ postgres resource has auto-generated "postgres-password" parameter
                      └─ In CI: no stored value in IConfiguration["Parameters:postgres-password"]
                           └─ ParameterProcessor calls HandleUnresolvedParametersAsync
                                └─ InteractionService.IsAvailable == true  ← ROOT CAUSE
                                     └─ PromptInputsAsync waits on CompletionTcs forever
                                          └─ HANG (nobody to complete it — no TTY in CI)
```

### Why `IsAvailable` is `true` in CI

`InteractionService.IsAvailable` in the Aspire AppHost reads a specific environment variable:

```csharp
// src/Aspire.Hosting/InteractionService.cs
public bool IsAvailable {
    get {
        if (_distributedApplicationOptions.DisableDashboard) return false;
        var interactivityEnabled = _configuration[KnownConfigNames.InteractivityEnabled];
        // KnownConfigNames.InteractivityEnabled = "ASPIRE_INTERACTIVITY_ENABLED"
        if (!string.IsNullOrEmpty(interactivityEnabled)
            && bool.TryParse(interactivityEnabled, out var enabled) && !enabled)
            return false;
        return true;  // ← default when env var is absent
    }
}
```

`ASPIRE_INTERACTIVITY_ENABLED` was never set on the "Deploy application" step's subprocess. Therefore `IsAvailable` always returned `true`, causing the interactive prompt loop to activate even in a headless CI environment.

### Why `postgres-password` has no stored value

`AddPostgres` in `Program.cs` calls `CreateDefaultPasswordParameter`, which creates a `GenerateParameterDefault` (random, minLength 22). In **publish mode** (`IsRunMode = false`), the parameter value is not persisted to user secrets, so `IConfiguration["Parameters:postgres-password"]` is empty in a fresh CI runner.

When `ParameterProcessor.ProcessParameterAsync` cannot find the value in configuration, the parameter is added to `_unresolvedParameters`. With `IsAvailable = true`, `HandleUnresolvedParametersAsync` then enters the interactive resolution loop.

---

## Why Previous Mitigations Did Not Work

| Attempted fix | Why it failed |
|---|---|
| `DOTNET_ASPIRE_NONINTERACTIVE=true` | Controls CLI output/spinners only. **Not read** by the AppHost's `InteractionService`. |
| `--non-interactive` flag on `aspire deploy` | Controls CLI behavior. Does **not** set `ASPIRE_INTERACTIVITY_ENABLED` on the AppHost subprocess. |
| `--parameter postgres-password=X` | The `--parameter` CLI flag is not forwarded to the AppHost as `IConfiguration["Parameters:postgres-password"]`. The AppHost reads configuration, not CLI args passed through the parent process. Passing unknown parameter names can itself cause the hang to manifest differently. |
| `timeout-minutes: 30` on the step | Fails fast instead of running indefinitely — a safety net, not a fix. |

---

## The Fix

Set `ASPIRE_INTERACTIVITY_ENABLED: "false"` in the `env:` block of the "Deploy application" step in both workflow files. This variable is inherited by the AppHost subprocess launched by `aspire deploy`.

```yaml
- name: Deploy application
  id: deploy
  timeout-minutes: 30
  working-directory: ./backend/MfePortal.AppHost
  run: |
    aspire deploy \
      --environment-name "${{ env.AZURE_ENV_NAME }}" \
      --non-interactive \
      ...
  env:
    ASPIRE_INTERACTIVITY_ENABLED: "false"   # ← THE FIX
    Azure__SubscriptionId: ${{ env.AZURE_SUBSCRIPTION_ID }}
    Azure__ResourceGroup: ${{ env.AZURE_RESOURCE_GROUP }}
    Azure__Location: ${{ env.AZURE_LOCATION }}
```

### What changes with the fix

When `IsAvailable = false`:

- `ParameterProcessor` does **not** enter the interactive resolution loop.
- Parameters with `GenerateParameterDefault` (like `postgres-password`) have their value generated automatically and the pipeline proceeds.
- Parameters with no value and no default fail fast with a logged `MissingParameterValueException` — much better than a silent hang.

---

## Aspire Source Files Consulted

| File | Purpose |
|---|---|
| `src/Aspire.Hosting/Pipelines/DistributedApplicationPipeline.cs` | Defines the `process-parameters` step; calls `ParameterProcessor.InitializeParametersAsync` |
| `src/Aspire.Hosting/Pipelines/WellKnownPipelineSteps.cs` | Defines `ProcessParameters = "process-parameters"` |
| `src/Aspire.Hosting/Orchestrator/ParameterProcessor.cs` | `HandleUnresolvedParametersAsync` loops on `PromptInputsAsync` when `IsAvailable = true` |
| `src/Aspire.Hosting/InteractionService.cs` | `IsAvailable` property; reads `ASPIRE_INTERACTIVITY_ENABLED` |
| `src/Shared/KnownConfigNames.cs` | `InteractivityEnabled = "ASPIRE_INTERACTIVITY_ENABLED"` |
| `src/Aspire.Hosting/ParameterResourceBuilderExtensions.cs` | `CreateDefaultPasswordParameter`, `GetParameterValue` logic |
| `src/Aspire.Hosting/ApplicationModel/ParameterResource.cs` | `ValueInternal`, `WaitForValueTcs` |
| `src/Aspire.Hosting.PostgreSQL/PostgresBuilderExtensions.cs` | `AddPostgres` creates `postgres-password` via `CreateDefaultPasswordParameter` |

All files read from `https://raw.githubusercontent.com/microsoft/aspire/main/`.

---

## Commits

| Commit | Change |
|---|---|
| `84e1578` | Add `--non-interactive` and `Azure__` env vars to CD deploy steps |
| `9dc9dcb` | Upgrade Aspire CLI to 13.2.0, add `--non-interactive` to prod deploy |
| `fb44be6` | Add trace logging and exception details to CD test deploy |
| `f76e173` | Remove `--parameter` flags, add `timeout-minutes: 30`, enable debug logging |
| `d774e0b` | **Set `ASPIRE_INTERACTIVITY_ENABLED=false` — the actual fix** |
