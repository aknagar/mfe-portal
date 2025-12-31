var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AugmentService>("augmentservice");

builder.Build().Run();
