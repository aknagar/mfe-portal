#:package Aspire.Hosting.Azure.AppContainers@13.1.0
#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

// Add the following line to configure the Azure App Container environment
builder.AddAzureContainerAppEnvironment("env");

var frontend = builder.AddDockerfile("frontend", "./", "./Dockerfile")
    .WithHttpEndpoint(port: 80, targetPort: 1234)
    .WithExternalHttpEndpoints();

builder.Build().Run();
