using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

const string Name = "infra";  // keep the name short and lowercase, as it may be used in resource names and URLs

// Detect if we're running in Azure provisioning mode
// Azure Container Apps requires HTTP endpoints to use port 80
bool isAzureProvisioning = args.Contains("--publisher") ||
                          Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") != null;

var containerAppEnvironment = builder.AddAzureContainerAppEnvironment(Name);

// Use Redis as local container in development, provision as Azure Redis Cache in Azure
var daprRedis = builder.AddAzureManagedRedis("daprRedis");

if (builder.Environment.IsDevelopment())
{
    daprRedis.RunAsContainer();
}

var redisHost = daprRedis.Resource.HostName;
var redisPort = daprRedis.Resource.Port;
var redisPassword = daprRedis.Resource.Password;

// PubSub component backed by Redis.
//
// How dynamic host/password injection works:
//   1. WithMetadata("redisHost", ReferenceExpression) adds a DaprComponentValueProviderAnnotation,
//      which causes Aspire to inject PUBSUB_REDISHOST=<host>:<port> into the Dapr CLI process env.
//   2. WithMetadata("redisPassword", ReferenceExpression) similarly injects PUBSUB_REDISPASSWORD.
//   3. The Dapr sidecar loads the secretstores.local.env secret store, which reads from env vars,
//      so PUBSUB_REDISHOST and PUBSUB_REDISPASSWORD are available as secrets at runtime.
//   4. The pubsub YAML (read from .dapr/components/pubsub.yaml in the AppHost directory) references
//      both values via secretKeyRef and declares auth.secretStore: secretstore.
//
// NOTE: LocalPath must NOT be set — when LocalPath is provided the Aspire lifecycle hook passes
// the YAML file to the Dapr CLI verbatim and skips ALL WithMetadata() transformations, so the
// PUBSUB_REDISHOST env var is never injected, causing a fatal "connecting to redis at :" error.
//
// NOTE: The CommunityToolkit Aspire Dapr package only auto-adds auth.secretStore to the generated
// YAML when WithMetadata(ParameterResource) is used, not when WithMetadata(IValueProvider) is used.
// We work around this bug by providing our own pubsub.yaml in .dapr/components/ (in the AppHost
// directory), which the toolkit reads as a base template and preserves the auth block from.
//
// NOTE: AddRedis() (used by RunAsContainer) always generates a random password for the Redis container.
// We must inject it so Dapr can authenticate. In Azure mode with Entra ID, Password is null (no auth needed).
var pubSubBuilder = builder.AddDaprPubSub("pubsub")
                    .WithMetadata("redisHost", ReferenceExpression.Create(
                        $"{redisHost}:{redisPort}"
                    ))
                    .WithMetadata("enableTLS", "true");

if (redisPassword is not null)
{
    pubSubBuilder.WithMetadata("redisPassword", redisPassword);
}

var pubSub = pubSubBuilder;

// State store - uses in-memory provider for local development (no Redis needed).
// For production, configure a persistent state store (e.g. state.redis or state.azure.cosmosdb).
var stateStore = builder.AddDaprStateStore("statestore");

var postgres = builder.AddPostgres("postgres")
                .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var productdb = postgres.AddDatabase("productdb", "productdb");
var weatherdb = postgres.AddDatabase("weatherdb", "weatherdb");


// Add Azure Service Bus - use emulator in development
var serviceBus = builder.AddAzureServiceBus("messaging");

if (builder.Environment.IsDevelopment())
{
    serviceBus.RunAsEmulator();
}

// Add queues for Dapr pubsub
var serviceBusQueue = serviceBus.AddServiceBusQueue("orders");

// Add AugmentService.Api with references
var augmentService = builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithDaprSidecar(sidecar => sidecar.WithReference(stateStore).WithReference(pubSub))
    .WithReference(productdb)
    .WithReference(weatherdb)
    .WithReference(serviceBus)    
    .WithExternalHttpEndpoints()
    .WaitFor(productdb)
    .WaitFor(weatherdb)
    .WaitFor(serviceBus)
    .WaitFor(daprRedis);

// Only add Application Insights and Key Vault in non-development environments
if (!builder.Environment.IsDevelopment())
{
    var logAnalyticsWorkspace = builder.AddAzureLogAnalyticsWorkspace($"logs-{Name}");
    containerAppEnvironment.WithAzureLogAnalyticsWorkspace(logAnalyticsWorkspace);

    // Add Application Insights - Aspire will manage provisioning
    var appInsights = builder.AddAzureApplicationInsights($"appinsights-{Name}", logAnalyticsWorkspace);
    augmentService.WithReference(appInsights);
    
    // Add Key Vault - no provisioning, uses existing vault via configuration
    var keyVault = builder.AddAzureKeyVault("keyvault")
                    .PublishAsConnectionString();

    augmentService.WithReference(keyVault);
}

// Azure Container Apps requires HTTP endpoints to use port 80
// In local development, use port 1234 for convenience
// In Azure provisioning, always use port 80 (required by Azure Container Apps)
var frontendPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 1234 : 80);

var frontend = builder.AddDockerfile("frontend", "../../frontend", "Dockerfile")
    .WithHttpEndpoint(port: frontendPort, targetPort: 1234, name: "http")
    .WithExternalHttpEndpoints()
    .WaitFor(augmentService);

// Add Diagrid Dashboard for Dapr monitoring
// Azure Container Apps requires HTTP endpoints to use port 80
// In local development, use port 8080 for convenience
// In Azure provisioning, always use port 80 (required by Azure Container Apps)
var diagridPort = isAzureProvisioning ? 80 : (builder.Environment.IsDevelopment() ? 8080 : 80);

var diagridDashboard = builder.AddContainer("diagrid-dashboard", "ghcr.io/diagridio/diagrid-dashboard:0.0.1")
    .WithHttpEndpoint(port: diagridPort, targetPort: 8080, name: "http")
    .WithExternalHttpEndpoints();

// Add k6 load testing only in development
if (builder.Environment.IsDevelopment())
{
    var k6 = builder.AddK6("k6")
                .WithBindMount("../tests/k6/scripts", "/scripts", isReadOnly: true)
                .WithScript("/scripts/main.js")
                .WithReference(augmentService) // Aspire then injects environment variables into the k6 container, one per exported endpoint of myService. convention: services__{resourceName}__{bindingName}__{index}
                .WaitFor(augmentService);
}

builder.Build().Run();
