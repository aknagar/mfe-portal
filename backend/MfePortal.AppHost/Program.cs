var builder = DistributedApplication.CreateBuilder(args);

var augmentService = builder.AddProject<Projects.AugmentService>("augmentservice");

builder.Build().Run();
