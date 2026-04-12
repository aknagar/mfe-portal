using Aspire.Hosting.Azure.AppContainers;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Load .env.local from the backend/ directory (one level up from AppHost) in development only.
// This file is gitignored — copy backend/.env.example to backend/.env.local and fill in real values.
// In production, values are supplied as Bicep parameters via azd.
if (builder.Environment.IsDevelopment())
{
    var envLocalPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".env.local"));
    if (File.Exists(envLocalPath))
    {
        var envVars = File.ReadLines(envLocalPath)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
            .Select(line => line.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Replace("__", ":"), parts => parts[1]);

        builder.Configuration.AddInMemoryCollection(envVars!);
    }
}

// Azure AD parameters — resolved from .env.local (local dev), user-secrets, or Bicep parameters (deploy).
// In .env.local use: Parameters__AzureAdTenantId=your-value
var azureAdTenantId = builder.AddParameter("AzureAdTenantId");
var azureAdClientId = builder.AddParameter("AzureAdClientId");

const string Name = "infra";  // keep short and lowercase — used in Azure resource names and URLs

// Azure Container Apps requires port 80; detect publish mode for port selection
bool isAzureProvisioning = args.Contains("--publisher") ||
                          Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") != null;

var containerAppEnvironment = builder.AddAzureContainerAppEnvironment(Name);

var daprRedis = builder.AddAzureManagedRedis("daprRedis");

if (builder.Environment.IsDevelopment())
{
    daprRedis.RunAsContainer();
}

var redisHost = daprRedis.Resource.HostName;
var redisPort = daprRedis.Resource.Port;
var redisPassword = daprRedis.Resource.Password;

var postgres = builder.AddAzurePostgresFlexibleServer("postgres");

if (builder.Environment.IsDevelopment() && !builder.ExecutionContext.IsPublishMode)
{
    postgres.RunAsContainer(c => c.WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8"));
}

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");
// Dedicated database for the Dapr statestore component.
// Keeps Dapr's internal tables (dapr_state, dapr_metadata) separate from application databases
// and allows independent access-control tuning.
var daprstate = postgres.AddDatabase("daprstate", "daprstate");

// Register Dapr components (pubsub and statestore) on the ACA managed environment.
// This is only needed when publishing Bicep — in dev, the local YAML files under
// .dapr/components/ are used instead via `dapr run`.
//
// The CommunityToolkit Dapr package does not generate these ACA component resources
// automatically, so we inject them via ConfigureInfrastructure. This generates
// Microsoft.App/managedEnvironments/daprComponents child resources in the infra Bicep module
// alongside the Aspire Dashboard dotnet component.
//
// IMPORTANT: Guard with IsPublishMode (not IsDevelopment) so this callback fires during
// `aspire publish` / `aspire deploy` Bicep generation. Using IsDevelopment() was incorrect
// because Aspire generates both infra.module.bicep (used by aspire deploy) and infra/infra.bicep
// (used by az deployment sub create) using the same ConfigureInfrastructure delegate — and that
// delegate is only appended when the condition is true at build time. IsDevelopment() is false
// at runtime in CI but the manifest-writing phase runs before the environment is fully resolved,
// causing infra.module.bicep to be generated without the Dapr component resources.
if (builder.ExecutionContext.IsPublishMode)
{
    containerAppEnvironment.ConfigureInfrastructure(infra =>
    {
        var env = infra.GetProvisionableResources()
                       .OfType<ContainerAppManagedEnvironment>()
                       .Single();

        // The Redis hostname is a BicepOutputReference from the daprRedis module.
        // AsProvisioningParameter() threads it as a Bicep parameter into this module so that
        // main.bicep can pass it as an inter-module output reference automatically.
        var redisHostParam = daprRedis.Resource.HostName.AsProvisioningParameter(infra, "daprRedis_outputs_hostname");

        // Redis Enterprise uses port 10000 for TLS. We concat inline because Bicep
        // does not support string interpolation on parameter references in resource properties.
        var redisEndpoint = BicepFunction.Concat(redisHostParam, ":10000");

        // IMPORTANT: Set .Name explicitly to the exact string Dapr uses to look up the component.
        // Without .Name, Azure.Provisioning generates take('pubsub${uniqueString(...)}', 24)
        // as the ARM resource name. The Dapr runtime matches components by their exact ARM name,
        // so a randomised name would cause all Dapr client calls targeting "pubsub"/"statestore"
        // to silently fail with "component not found".
        infra.Add(new ContainerAppManagedEnvironmentDaprComponent("daprPubSub")
        {
            Name = "pubsub",
            ComponentType = "pubsub.redis",
            Version = "v1",
            Metadata =
            [
                new ContainerAppDaprMetadata { Name = "redisHost", Value = redisEndpoint },
                new ContainerAppDaprMetadata { Name = "enableTLS",  Value = "true" },
                // Instruct the Dapr Redis component to authenticate via Azure Managed Identity
                // (DefaultAzureCredential) instead of an access key.
                new ContainerAppDaprMetadata { Name = "useEntraID", Value = "true" },
            ],
            // Scope the component to augmentservice only — avoids cross-app component leakage.
            Scopes = ["augmentservice"],
            Parent = env,
        });

        // State store backed by Azure PostgreSQL Flexible Server (state.postgresql/v2).
        // useAzureAD=true instructs the Dapr sidecar to authenticate via DefaultAzureCredential
        // (the augmentservice managed identity) — no password is stored or transmitted.
        // The postgres Flexible Server has passwordAuth=Disabled / activeDirectoryAuth=Enabled,
        // so Entra ID is the only permitted auth method.
        // The connection string contains only the host — no password field is included; Dapr
        // acquires a short-lived access token at runtime via the managed identity.
        // Thread the postgres hostname as a Bicep parameter into this module so that main.bicep
        // can wire the inter-module output reference automatically (same pattern as redisHostParam).
        var postgresHostParam = postgres.Resource.HostName.AsProvisioningParameter(infra, "postgres_outputs_hostname");

        // Build a libpq-format connection string with no password field.
        // useAzureAD=true (below) tells the Dapr sidecar to acquire an Entra ID access token
        // via DefaultAzureCredential and present it as the password — no secret needed here.
        // The user must match the Entra ID principal name of the augmentservice managed identity
        // that is registered as a PostgreSQL AAD administrator.
        var postgresConnStr = BicepFunction.Concat(
            "host=", postgresHostParam, " user=augmentservice sslmode=require dbname=daprstate"
        );

        infra.Add(new ContainerAppManagedEnvironmentDaprComponent("daprStateStore")
        {
            Name = "statestore",
            ComponentType = "state.postgresql/v2",
            Version = "v1",
            Metadata =
            [
                new ContainerAppDaprMetadata { Name = "connectionString", Value = postgresConnStr },
                // useAzureAD=true — authenticate with the augmentservice managed identity
                // via DefaultAzureCredential; no password is needed or transmitted.
                new ContainerAppDaprMetadata { Name = "useAzureAD", Value = "true" },
                // actorStateStore=true is required by the Dapr Workflow engine, which uses
                // the actor runtime and relies on having a dedicated actor state store.
                new ContainerAppDaprMetadata { Name = "actorStateStore", Value = "true" },
            ],
            Scopes = ["augmentservice"],
            Parent = env,
        });
    });
}

// PubSub backed by Redis. WithMetadata() injects PUBSUB_REDISHOST / PUBSUB_REDISPASSWORD into the
// Dapr CLI process env; the local.env secret store exposes them to the component YAML via secretKeyRef.
// NOTE: LocalPath must NOT be set — it bypasses WithMetadata() and the env vars are never injected.
// NOTE: auth.secretStore is declared in .dapr/components/pubsub.yaml because the toolkit only
//       auto-adds it when WithMetadata(ParameterResource) is used, not IValueProvider.
//
// IMPORTANT: WithMetadata(IValueProvider) is skipped in publish mode because resolving
// BicepOutputReference values (redisHost, redisPort, redisPassword) requires awaiting
// ProvisioningTaskCompletionSource, which only completes in the 'provision-*' pipeline step —
// AFTER 'process-parameters'. Calling GetValueAsync() during process-parameters causes a
// silent deadlock and the publish pipeline hangs indefinitely. The Dapr sidecar CLI does not
// run in publish mode so these metadata values are not needed in the manifest.
var pubSubBuilder = builder.AddDaprPubSub("pubsub")
                    .WithMetadata("enableTLS", "true");

if (!builder.ExecutionContext.IsPublishMode)
{
    pubSubBuilder
        .WithMetadata("redisHost", ReferenceExpression.Create(
            $"{redisHost}:{redisPort}"
        ));

    if (redisPassword is not null)
    {
        // RunAsContainer() generates a random Redis password; inject it for Dapr auth.
        // In Azure (Entra ID mode) Password is null — no password needed.
        pubSubBuilder.WithMetadata("redisPassword", redisPassword);
    }
}

var pubSub = pubSubBuilder;

// State store backed by Azure PostgreSQL Flexible Server (state.postgresql/v2).
// WithMetadata() injects STATESTORE_CONNECTIONSTRING into the Dapr CLI process env;
// the local.env secret store exposes it to the component YAML via secretKeyRef.
//
// In local dev the containerised Postgres connection string includes a password
// (RunAsContainer generates one). In Azure, useAzureAD=true is set in
// ConfigureInfrastructure above — the connection string passed here at dev time has
// no effect in publish mode (see publish-mode guard below).
//
// NOTE: The file is named state.yaml (the component type name), not statestore.yaml —
//       CommunityToolkit.Aspire.Hosting.Dapr probes by type name, not resource name.
// NOTE: Same publish-mode guard as pubsub — BicepOutputReference resolution deadlocks
//       in publish mode, so WithMetadata() is skipped there.
var stateStoreBuilder = builder.AddDaprStateStore("statestore");

if (!builder.ExecutionContext.IsPublishMode)
{
    // PostgresDatabaseResource.ConnectionStringExpression returns a PostgreSQL URL:
    //   postgresql://user:password@host:port/database
    // This is the format Dapr's pgx driver (state.postgresql/v2) expects — the Npgsql
    // ADO.NET format ("Host=...;Port=...;Database=...") is NOT accepted by pgx.
    // Referencing daprstate.Resource (IValueProvider) resolves the URL at startup time.
    stateStoreBuilder.WithMetadata("connectionString", daprstate.Resource);
}

var stateStore = stateStoreBuilder;

var serviceBus = builder.AddAzureServiceBus("messaging");

if (builder.Environment.IsDevelopment())
{
    serviceBus.RunAsEmulator();
}

var serviceBusQueue = serviceBus.AddServiceBusQueue("orders");

var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithDaprSidecar(sidecar => sidecar.WithReference(stateStore).WithReference(pubSub))
    .WithReference(productdb)
    .WithReference(weatherdb)
    .WithReference(serviceBus)
    .WithExternalHttpEndpoints()
    .WaitFor(productdb)
    .WaitFor(weatherdb)
    .WaitFor(daprstate)
    .WaitFor(serviceBus)
    .WaitFor(daprRedis)
    .WithEnvironment("AzureAd__TenantId", azureAdTenantId)
    .WithEnvironment("AzureAd__ClientId", azureAdClientId)
    .WithEnvironment("AzureAd__Audience", ReferenceExpression.Create($"api://{azureAdClientId}"))
    // Inject the Dapr sidecar configuration into the generated ACA container app Bicep.
    // The CommunityToolkit Dapr package does not emit this automatically for ACA deployments,
    // so we must set it explicitly via PublishAsAzureContainerApp. The app port must match
    // the HTTP_PORTS / targetPort value Aspire assigns to this container app.
    // ACA Aspire assigns port 8080 as the default container port for .NET projects.
    .PublishAsAzureContainerApp((infra, app) =>
    {
        app.Configuration.Dapr = new ContainerAppDaprConfiguration
        {
            IsEnabled = true,
            AppId = "augmentservice",
            AppProtocol = ContainerAppProtocol.Http,
            AppPort = 8080,
        };
    });

if (!builder.Environment.IsDevelopment())
{
    var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace($"logs-{Name}");
    containerAppEnvironment.WithAzureLogAnalyticsWorkspace(logAnalyticsWorkspace);

    var appInsights = builder.AddAzureApplicationInsights($"appinsights-{Name}", logAnalyticsWorkspace);
    augmentService.WithReference(appInsights);

    // Key Vault uses an existing vault — no Aspire provisioning, referenced via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();

    augmentService.WithReference(keyVault);
}

// Azure Container Apps requires port 80; use friendly ports locally
var frontendPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 1234 : 80);

var frontend = builder.AddDockerfile("frontend", "../../frontend", "Dockerfile")
    .WithHttpEndpoint(port: frontendPort, targetPort: 1234, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(augmentService);

var diagridPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 8080 : 80);

var diagridDashboard = builder.AddContainer("diagrid-dashboard", "ghcr.io/diagridio/diagrid-dashboard:0.0.1")
    .WithHttpEndpoint(port: diagridPort, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints();

// k6 is excluded from publish mode: WithScript() passes virtualUsers as a raw int to WithArgs(),
// which the ACA manifest publisher's ProcessValue() does not support.
if (builder.Environment.IsDevelopment() && !builder.ExecutionContext.IsPublishMode)
{
    var k6 = builder.AddK6("k6")
                .WithBindMount("../tests/k6/scripts", "/scripts", isReadOnly: true)
                .WithScript("/scripts/main.js")
                .WithReference(augmentService)
                .WaitFor(augmentService);
}

builder.Build().Run();
