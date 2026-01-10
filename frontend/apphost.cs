#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

var frontend = builder.AddViteApp("frontend", "../frontend", runScriptName: "start")
    .WithExternalHttpEndpoints() // to mark it as publicly accessible.
    .PublishAsDockerFile();  // This generates a Dockerfile during publish

builder.Build().Run();
