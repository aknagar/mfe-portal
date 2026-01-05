var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--encoding=UTF8");

var postgresdb = postgres.AddDatabase("postgresdb", "productdb");

// Add AugmentService.Api with reference to local PostgreSQL database
// Azure Key Vault will be accessed via DefaultAzureCredential in the service
builder.AddProject<Projects.AugmentService_Api>("augmentservice")
    .WithReference(postgresdb)
    .WaitFor(postgresdb);

builder.Build().Run();
