#:package Aspire.Hosting.Azure.AppContainers@13.1.0
#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add the following line to configure the Azure App Container environment
builder.AddAzureContainerAppEnvironment("infra");

var frontend = builder.AddDockerfile("frontend", "./", "./Dockerfile")
    .WithHttpEndpoint(port: builder.Environment.IsDevelopment() ? 1234 : 80, targetPort: 1234, name: "http")
    .WithExternalHttpEndpoints()
    .PublishAsContainer();

builder.Build().Run();
