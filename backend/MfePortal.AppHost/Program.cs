var builder = DistributedApplication.CreateBuilder(args);

// Configure Dapr environment for running services
builder.Services.Configure<HostingOptions>(options =>
{
    // These can be overridden by environment variables
});

var augmentService = builder
    .AddProject<Projects.AugmentService>("augmentservice")
    .WithEnvironment("DAPR_HTTP_ENDPOINT", "http://localhost:3500")
    .WithEnvironment("DAPR_GRPC_ENDPOINT", "http://localhost:50001");

builder.Build().Run();
