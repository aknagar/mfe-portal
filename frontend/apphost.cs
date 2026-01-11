#:package Aspire.Hosting.Azure.AppContainers@13.1.0
#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

// Add the following line to configure the Azure App Container environment
builder.AddAzureContainerAppEnvironment("env");

builder.AddViteApp("frontend", "../frontend/shell", runScriptName: "start")
    .WithExternalHttpEndpoints() // to mark it as publicly accessible.
    .WithHttpHealthCheck("/") // Add health check endpoint
    .PublishAsDockerFile();  // This generates a Dockerfile during publish. It needs Dockerfile.

builder.Build().Run();
