using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Load .env.local from the backend/ directory (one level up from AppHost) in development only.
// This file is gitignored — copy backend/.env.example to backend/.env.local and fill in real values.
// In production, environment variables are injected directly by the container runtime.
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

// In-memory state store for local development. Replace with a persistent provider for production.
var stateStore = builder.AddDaprStateStore("statestore");

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

// AzureAd values are loaded from .env.local (local dev) or container environment variables (production).
// Keys in .env.local use double-underscore notation (AzureAd__TenantId) which is normalised to
// AzureAd:TenantId by the loader above — matching ASP.NET Core's configuration hierarchy convention.
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
    .WithEnvironment("AzureAd__TenantId", builder.Configuration["AzureAd:TenantId"] ?? string.Empty)
    .WithEnvironment("AzureAd__ClientId", builder.Configuration["AzureAd:ClientId"] ?? string.Empty)
    .WithEnvironment("AzureAd__Audience", $"api://{builder.Configuration["AzureAd:ClientId"] ?? string.Empty}");

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
