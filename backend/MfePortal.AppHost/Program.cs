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

// Register Dapr components (pubsub and statestore) on the ACA managed environment.
// This is only needed when deploying to Azure — in dev, the local YAML files under
// .dapr/components/ are used instead via `dapr run`.
//
// The CommunityToolkit Dapr package does not generate these ACA component resources
// automatically, so we inject them via ConfigureInfrastructure. This generates
// Microsoft.App/managedEnvironments/daprComponents child resources in the infra Bicep module
// alongside the Aspire Dashboard dotnet component.
//
// Authentication uses Azure Managed Identity (Entra ID) — no access keys required.
// The Redis Enterprise instance has accessKeysAuthentication disabled; the augmentservice
// managed identity is granted the Redis data access policy via a separate role assignment
// module (augmentservice-roles-daprRedis). We pass useEntraID=true so the Dapr sidecar
// uses DefaultAzureCredential for Redis authentication.
if (!builder.Environment.IsDevelopment())
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

        infra.Add(new ContainerAppManagedEnvironmentDaprComponent("pubsub")
        {
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

        infra.Add(new ContainerAppManagedEnvironmentDaprComponent("statestore")
        {
            ComponentType = "state.redis",
            Version = "v1",
            Metadata =
            [
                new ContainerAppDaprMetadata { Name = "redisHost", Value = redisEndpoint },
                new ContainerAppDaprMetadata { Name = "enableTLS",  Value = "true" },
                new ContainerAppDaprMetadata { Name = "useEntraID", Value = "true" },
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

// State store backed by the same Redis instance as pubsub.
// WithMetadata() injects STATESTORE_REDISHOST / STATESTORE_REDISPASSWORD into the Dapr CLI process
// env; the local.env secret store exposes them to the component YAML via secretKeyRef.
// The component YAML (.dapr/components/statestore.yaml) sets actorStateStore: "true", which is
// required by the Dapr Workflow engine (it uses the actor runtime underneath).
// NOTE: Same publish-mode guard as pubsub — BicepOutputReference resolution deadlocks in publish mode.
var stateStoreBuilder = builder.AddDaprStateStore("statestore")
                               .WithMetadata("enableTLS", "true");

if (!builder.ExecutionContext.IsPublishMode)
{
    stateStoreBuilder
        .WithMetadata("redisHost", ReferenceExpression.Create(
            $"{redisHost}:{redisPort}"
        ));

    if (redisPassword is not null)
    {
        stateStoreBuilder.WithMetadata("redisPassword", redisPassword);
    }
}

var stateStore = stateStoreBuilder;

var postgres = builder.AddPostgres("postgres")
                .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");

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
