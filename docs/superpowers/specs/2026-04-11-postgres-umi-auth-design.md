# Design: Migrate Postgres to Azure Flexible Server with User-Managed Identity Auth

**Date:** 2026-04-11  
**Status:** Approved  
**Motivation:** KEDA `KEDAScalerFailed` error — `error resolving secret connectionstrings--productdb` — because KEDA's postgresql scaler cannot resolve ACA-scoped secrets. Fix by eliminating the password secret entirely: migrate from a self-hosted Docker postgres container to Azure Database for PostgreSQL Flexible Server with Entra ID / User-Managed Identity (UMI) authentication.

---

## Problem

The `augmentservice` ACA container app currently connects to a plain Docker postgres container (`docker.io/library/postgres:17.6`) also running in ACA. The connection string is stored as an ACA secret (`connectionstrings--productdb`) and referenced via `secretRef` in the container env. KEDA's postgresql scaler for `augmentservice` references this secret but cannot resolve it because ACA secrets are scoped to the container app, not to external KEDA infrastructure. The result is a recurring `KEDAScalerFailed` event that prevents KEDA from scaling the app.

---

## Goals

- Replace password-based postgres auth with Entra ID / UMI authentication.
- Eliminate all postgres-related ACA secrets (`connectionstrings--productdb`, `productdb-password`, `productdb-uri`, `connectionstrings--weatherdb`, `weatherdb-password`, `weatherdb-uri`).
- Inject `ConnectionStrings__productdb` and `ConnectionStrings__weatherdb` as plain env vars (no `secretRef`).
- Unblock KEDA's postgresql scaler by switching its `TriggerAuthentication` to `azure-managed-identity`.
- Maintain local dev experience: Docker postgres container via `RunAsContainer()` unchanged.

---

## Non-Goals

- Changing any other resource (Redis, Service Bus, Key Vault) authentication — all already use UMI.
- Migrating EF Core migrations — schema stays the same; only the connection mechanism changes.
- Changing integration test infra — tests use `Testcontainers.PostgreSql` and are unaffected.

---

## Architecture

### Resource: Azure Database for PostgreSQL Flexible Server

**Key finding from Aspire 13.2.0 source:** `AddAzurePostgresFlexibleServer` defaults to Entra ID authentication — no `.WithPasswordAuthentication(false)` call is needed. The current Bicep output is wrong (shows a Docker container) because `RunAsContainer()` is called inside a `builder.Environment.IsDevelopment()` guard. In publish mode Aspire's manifest phase is not "Development", so the guard is false and `RunAsContainer()` is NOT called — yet the manifest still shows `container.v1`. This is because Aspire generates the manifest while the AppHost runs, and in a local `aspire publish` invocation `IsDevelopment()` is true, so `RunAsContainer()` still fires and overrides the Azure Flexible Server path.

**Fix:** Guard `RunAsContainer()` with `!builder.ExecutionContext.IsPublishMode` (same pattern already used for Dapr components and k6 in this project). When publish mode is active, `RunAsContainer()` is skipped and Aspire generates the full Azure Flexible Server Bicep.

With this guard in place, Aspire automatically:

1. Generates a `Microsoft.DBforPostgreSQL/flexibleServers` Bicep resource with:
   - `authConfig.activeDirectoryAuth = Enabled`
   - `authConfig.passwordAuth = Disabled`
2. Generates a child `Microsoft.DBforPostgreSQL/flexibleServers/administrators` resource pointing at `augmentservice_identity` (the existing UMI) as the AD admin.
3. Generates an `augmentservice-roles-postgres` Bicep module that grants `augmentservice_identity` the necessary PostgreSQL Flexible Server AD permissions.
4. Emits a passwordless connection string as a Bicep output (not a secret):
   ```
   Host=<server-fqdn>.postgres.database.azure.com;Username=<umi-display-name>;Database=<db>;Authentication=ManagedIdentityAuthentication
   ```
5. Removes the `postgres-password` parameter from the manifest and all downstream Bicep params.

The `postgres` Bicep module changes from a `container.v1` / `Microsoft.App/containerApps` resource to a `Microsoft.DBforPostgreSQL/flexibleServers` resource. `main.bicep` gains a `postgres` module reference and an `augmentservice-roles-postgres` module reference.

### Connection String: Before vs. After

| Property | Before | After |
|---|---|---|
| ACA secret `connectionstrings--productdb` | `Host=postgres;Port=5432;Username=postgres;Password=<secret>` | **removed** |
| ACA secret `connectionstrings--weatherdb` | `Host=postgres;Port=5432;Username=postgres;Password=<secret>` | **removed** |
| ACA secret `productdb-password` | `<password>` | **removed** |
| ACA secret `weatherdb-password` | `<password>` | **removed** |
| ACA secret `productdb-uri` | `postgresql://postgres:<encoded-pw>@postgres:5432/productdb` | **removed** |
| ACA secret `weatherdb-uri` | `postgresql://postgres:<encoded-pw>@postgres:5432/weatherdb` | **removed** |
| Env `ConnectionStrings__productdb` | `secretRef: connectionstrings--productdb` | plain value: `Host=<fqdn>;Username=<umi-name>;Database=productdb;Authentication=ManagedIdentityAuthentication;GssEncryptionMode=Disable` |
| Env `ConnectionStrings__weatherdb` | `secretRef: connectionstrings--weatherdb` | plain value: same pattern for weatherdb |

### Npgsql / EF Core: Token Provider

Npgsql supports Entra ID token authentication via the `Npgsql.Azure` NuGet package (provides `UseAzure(TokenCredential)` on `NpgsqlDataSourceBuilder`). This installs a periodic password provider that:

1. Calls `DefaultAzureCredential.GetTokenAsync(new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]))`.
2. Passes the resulting short-lived access token as the PostgreSQL password.
3. Refreshes it before expiry.

**Changes to `Program.cs`:**
- Add `Npgsql.Azure` package to `AugmentService.Api.csproj` and `Directory.Packages.props`.
- Guard `postgres.RunAsContainer(...)` with `!builder.ExecutionContext.IsPublishMode` (same pattern as Dapr and k6 guards in this file).
- Pass `configureDataSourceBuilder: dsb => dsb.UseAzure(credential)` to **all three** `AddNpgsqlDbContext` calls:
  - `AddNpgsqlDbContext<ProductDataContext>` (connectionName: `"productdb"`)
  - `AddNpgsqlDbContext<WeatherDatabaseContext>` (connectionName: `"weatherdb"`)
  - `AddNpgsqlDbContext<AugmentService.Infrastructure.Data.UserDbContext>` (connectionName: `"weatherdb"`)
- Remove the `configureSettings` lambda (i.e. `s => s.ConnectionString += ";GssEncryptionMode=Disable"`) from all three calls. The `GssEncryptionMode=Disable` suffix will instead be appended to the connection string in the Aspire-generated Bicep env var (added to the `ConnectionStrings__productdb` / `ConnectionStrings__weatherdb` plain values). This keeps the setting but removes the runtime string-mutation pattern.

**Note:** The `DefaultAzureCredential` instance is already created in `Program.cs` and re-used for Key Vault. The same instance is passed to `UseAzure()`.

### KEDA: ACA Managed Identity Scale Rule

The KEDA postgresql scaler currently fails because it references the ACA secret `connectionstrings--productdb`. Once secrets are gone the scaler config must be updated.

**ACA-native KEDA pattern:** Azure Container Apps has its own built-in KEDA integration. You do **not** deploy raw KEDA `ScaledObject` or `TriggerAuthentication` CRDs in ACA. Instead, scaling rules are defined directly on the `Microsoft.App/containerApps` Bicep resource under `template.scale.rules`. A scale rule with `identity:` set to a UMI resource ID tells ACA's KEDA controller to authenticate to the target resource using that identity — no secret, no CRD.

`augmentservice.bicep` (regenerated in Task 4) gains a `scale.rules` block:

```bicep
scale: {
  minReplicas: 1
  rules: [
    {
      name: 'postgres-scaler'
      custom: {
        type: 'postgresql'
        metadata: {
          host: postgres_outputs_fqdn
          userName: postgres_outputs_adminlogin
          dbName: 'productdb'
          sslmode: 'require'
          targetQueryValue: '5'
          query: 'SELECT COUNT(*) FROM pg_stat_activity WHERE state = \'active\''
        }
        identity: augmentservice_identity_outputs_id
      }
    }
  ]
}
```

Two new params (`postgres_outputs_fqdn`, `postgres_outputs_adminlogin`) are added to `augmentservice.bicep` and wired through `main.bicep` from the `postgres` module outputs. The `augmentservice_identity_outputs_id` param already exists in the Aspire-generated `augmentservice.bicep` (used by the identity block).

### Local Dev: No Change

`RunAsContainer()` call remains but is now guarded with `!builder.ExecutionContext.IsPublishMode` (consistent with existing Dapr and k6 guards). When running locally (`IsDevelopment()` is true and `IsPublishMode` is false), `RunAsContainer()` fires as before and a Docker postgres container is used with password auth. When publishing (`IsPublishMode` is true), `RunAsContainer()` is skipped and Aspire generates the Azure Flexible Server Bicep with Entra ID auth.

---

## File Change Summary

| File | Change |
|---|---|
| `backend/Directory.Packages.props` | Add `Npgsql.Azure` version entry |
| `backend/AugmentService/AugmentService.Api/AugmentService.Api.csproj` | Add `<PackageReference Include="Npgsql.Azure" />` |
| `backend/MfePortal.AppHost/Program.cs` | Guard `postgres.RunAsContainer()` with `!IsPublishMode`; add `UseAzure(credential)` to all three `AddNpgsqlDbContext` calls; remove `configureSettings` lambdas |
| `backend/MfePortal.AppHost/aspire-output/` | Regenerated by `aspire publish` (Aspire-owned subdirectories): new `postgres/` Flexible Server bicep, `augmentservice-roles-postgres/` role module, updated `augmentservice/augmentservice.bicep` (secrets removed, plain env vars), updated `main.bicep` |
| `backend/MfePortal.AppHost/aspire-output/augmentservice/augmentservice.bicep` | Additionally hand-edited post-regeneration: add `scale.rules` with ACA managed identity postgres scaler; add `postgres_outputs_fqdn` and `postgres_outputs_adminlogin` params |
| `backend/MfePortal.AppHost/aspire-output/main.bicep` | Additionally hand-edited post-regeneration: wire `postgres_outputs_fqdn` and `postgres_outputs_adminlogin` into the `augmentservice` module params |

---

## Error Handling & Rollout Notes

- **Token fetch failure**: If `DefaultAzureCredential` cannot obtain a token (e.g., IMDS unavailable), `AddNpgsqlDbContext` will throw at connection time. The existing `isTest` guard in `Program.cs` already skips `AddNpgsqlDbContext` in the Test environment, so no regression there.
- **Flexible Server cold start**: Azure Flexible Server takes ~2–3 minutes to provision on first deploy. The `WaitFor(productdb)` / `WaitFor(weatherdb)` calls in AppHost already handle startup ordering.
- **Firewall**: Flexible Server must allow connections from the ACA environment's outbound IP range. Aspire's Flexible Server Bicep enables "Allow Azure services" by default via a firewall rule named `AllowAllAzureServicesAndResourcesWithinAzureDatacenter`.
- **KEDA scale rule identity**: The `augmentservice_identity_outputs_id` value wired into the scale rule's `identity:` field must be the full UMI **resource ID** (not the `clientId` or `principalId` GUID). ACA's KEDA controller resolves the managed identity by resource ID when authenticating to the external scaler target.

---

## Testing

- **Local dev**: No change. Docker postgres container continues to work.
- **Integration tests**: Use `Testcontainers.PostgreSql` with password auth — unaffected.
- **Deploy verification**: After `azd up`, check:
  1. `augmentservice` ACA app starts and health endpoint returns 200.
  2. No `KEDAScalerFailed` events in ACA diagnostics.
  3. Product and weather endpoints return data (confirming DB connectivity via UMI token).
