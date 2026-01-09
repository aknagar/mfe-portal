#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.JavaScript@13.1.0

var builder = DistributedApplication.CreateBuilder(args);

var frontend = builder.AddViteApp("frontend", "./", runScriptName: "start");

builder.Build().Run();
