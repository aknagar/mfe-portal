# MfePortal Backend

This solution contains the backend services for the MfePortal application, orchestrated by .NET Aspire.

## Projects

- **AugmentService**: A microservice that acts as a reverse proxy. It takes a URL as input, makes a request to it, logs the response, and returns the result.
- **MfePortal.AppHost**: The .NET Aspire orchestrator project.
- **MfePortal.ServiceDefaults**: Shared service configurations (OpenTelemetry, Health Checks, etc.).

## How to Run

1. Ensure you have the .NET Aspire workload installed:
   ```bash
   dotnet workload install aspire
   ```

2. Run the AppHost project:
   ```bash
   dotnet run --project MfePortal.AppHost/MfePortal.AppHost.csproj
   ```

3. The Aspire Dashboard will open (typically at https://localhost:15001). You can see the `augmentservice` running there.

4. You can test the AugmentService proxy endpoint:
   ```
   GET /proxy?url=https://example.com
   ```
