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

### KEDA: TriggerAuthentication → azure-managed-identity

The KEDA postgresql scaler currently fails because it references the ACA secret `connectionstrings--productdb`. Once secrets are gone the scaler config must be updated.

A new hand-authored Bicep module for the KEDA `TriggerAuthentication` and `ScaledObject` will be added as a flat file directly in `aspire-output/`, following the existing convention for hand-authored modules in this project (e.g. `augmentservice-roles-daprRedis.module.bicep`, `daprRedis.module.bicep`). The file will be named `augmentservice-keda-postgres-scaler.module.bicep`. It creates:

1. **`TriggerAuthentication`** (`keda.sh/v1alpha1`): Uses `azure-managed-identity` with the `augmentservice_identity` client ID. No secrets.
2. **`ScaledObject`**: References the `TriggerAuthentication`. The postgresql trigger connection string uses `sslmode=require` and `Authentication Method=azure managed identity` instead of a password field.

`main.bicep` gains a reference to this new module.

### Local Dev: No Change

`RunAsContainer()` call remains but is now guarded with `!builder.ExecutionContext.IsPublishMode` (consistent with existing Dapr and k6 guards). When running locally (`IsDevelopment()` is true and `IsPublishMode` is false), `RunAsContainer()` fires as before and a Docker postgres container is used with password auth. When publishing (`IsPublishMode` is true), `RunAsContainer()` is skipped and Aspire generates the Azure Flexible Server Bicep with Entra ID auth.

---

## File Change Summary

| File | Change |
|---|---|
| `backend/Directory.Packages.props` | Add `Npgsql.Azure` version entry |
| `backend/AugmentService/AugmentService.Api/AugmentService.Api.csproj` | Add `<PackageReference Include="Npgsql.Azure" />` |
| `backend/MfePortal.AppHost/Program.cs` | Guard `postgres.RunAsContainer()` with `!IsPublishMode`; add `UseAzure(credential)` to all three `AddNpgsqlDbContext` calls; remove `configureSettings` lambdas |
| `backend/MfePortal.AppHost/aspire-output/` | Regenerated by `aspire publish`: new `postgres/` Flexible Server bicep, `augmentservice-roles-postgres/` role module, updated `augmentservice/augmentservice.bicep` (secrets removed, plain env vars), updated `main.bicep` |
| `backend/MfePortal.AppHost/aspire-output/augmentservice-keda-postgres-scaler.module.bicep` | New hand-authored flat module file (matches `augmentservice-roles-daprRedis.module.bicep` naming convention) for KEDA `TriggerAuthentication` + `ScaledObject` |
| `backend/MfePortal.AppHost/aspire-output/main.bicep` | Add `augmentservice-keda-postgres-scaler` module reference |

---

## Error Handling & Rollout Notes

- **Token fetch failure**: If `DefaultAzureCredential` cannot obtain a token (e.g., IMDS unavailable), `AddNpgsqlDbContext` will throw at connection time. The existing `isTest` guard in `Program.cs` already skips `AddNpgsqlDbContext` in the Test environment, so no regression there.
- **Flexible Server cold start**: Azure Flexible Server takes ~2–3 minutes to provision on first deploy. The `WaitFor(productdb)` / `WaitFor(weatherdb)` calls in AppHost already handle startup ordering.
- **Firewall**: Flexible Server must allow connections from the ACA environment's outbound IP range. Aspire's Flexible Server Bicep enables "Allow Azure services" by default via a firewall rule named `AllowAllAzureServicesAndResourcesWithinAzureDatacenter`.
- **KEDA workload identity**: The `augmentservice_identity` UMI must be federated with the AKS/ACA KEDA operator service account if KEDA is running in a separate pod (standard ACA KEDA setup). This is handled by the `TriggerAuthentication` spec referencing the client ID.

---

## Testing

- **Local dev**: No change. Docker postgres container continues to work.
- **Integration tests**: Use `Testcontainers.PostgreSql` with password auth — unaffected.
- **Deploy verification**: After `azd up`, check:
  1. `augmentservice` ACA app starts and health endpoint returns 200.
  2. No `KEDAScalerFailed` events in ACA diagnostics.
  3. Product and weather endpoints return data (confirming DB connectivity via UMI token).
