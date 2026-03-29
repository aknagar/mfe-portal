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

// PubSub component - Aspire generates the YAML in-memory so that WithMetadata()
// can inject the dynamic Redis host/port resolved at runtime.
// NOTE: LocalPath must NOT be set here — when LocalPath is provided the Aspire
// lifecycle hook passes the file to the Dapr CLI verbatim (no transformation),
// so WithMetadata() annotations are never applied and ${REDIS_HOST} is never
// substituted, causing a fatal "connecting to redis at :" error at startup.
var pubSub = builder.AddDaprPubSub("pubsub")
                    .WithMetadata("redisHost", ReferenceExpression.Create(
                        $"{redisHost}:{redisPort}"
                    ))
                    .WaitFor(daprRedis);

// State store using Redis - same reasoning as pubSub above.
var stateStore = builder.AddDaprStateStore("statestore")
                        .WithMetadata("redisHost", ReferenceExpression.Create(
                            $"{redisHost}:{redisPort}"
                        ))
                        .WaitFor(daprRedis);

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
    .WithDaprSidecar(sidecar => sidecar.WithReference(stateStore).WithReference(pubSub).WaitFor(stateStore).WaitFor(pubSub))
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
