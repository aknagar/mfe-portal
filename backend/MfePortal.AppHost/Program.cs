var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AugmentService_Api>("augmentservice");

builder.Build().Run();
