# AugmentService API

A .NET 10.0 Web API built using ASP.NET Core with Clean Architecture principles, leveraging Aspire for cloud-native features and Dapr for workflows.

## Features

- **Clean Architecture**: Separated layers (Core, Application, Infrastructure, API)
- **Dapr Workflows**: Order processing with approval workflows
- **Azure Integration**: Key Vault, Service Bus, PostgreSQL
- **Rate Limiting**: Global throttling to protect against abuse
- **Authentication**: JWT Bearer authentication (development mode included)
- **OpenTelemetry**: Comprehensive logging, metrics, and tracing
- **Health Checks**: `/health` and `/alive` endpoints
- **OpenAPI**: Scalar UI for API documentation

## Rate Limiting

The AugmentService API implements global rate limiting to protect against abuse and ensure fair resource allocation.

### Configuration

Rate limiting is configured in `appsettings.json`:

```json
{
  "RateLimiting": {
    "Enabled": true,
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 2
  }
}
```

#### Configuration Options

- **Enabled**: Enable/disable rate limiting (default: `true`)
- **PermitLimit**: Maximum requests per window per client (default: `100`)
- **WindowSeconds**: Time window duration in seconds (default: `60`)
- **QueueLimit**: Number of requests that can be queued when limit is reached (default: `2`)

### Rate Limiting Strategy

- **Algorithm**: Fixed Window
- **Partition Key**: Authenticated user name or IP address
- **Exempt Endpoints**: `/health`, `/alive` (health checks are never rate limited)
- **Scope**: Global (applies to all API endpoints)

### HTTP 429 Response

When the rate limit is exceeded, clients receive a `429 Too Many Requests` response:

```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded. Please try again later.",
  "statusCode": 429,
  "retryAfterSeconds": 45
}
```

The response includes a `Retry-After` header indicating when to retry (in seconds).

### Environment-Specific Limits

#### Development (`appsettings.Development.json`)
```json
{
  "RateLimiting": {
    "Enabled": true,
    "PermitLimit": 200,
    "WindowSeconds": 60,
    "QueueLimit": 5
  }
}
```
- More permissive limits for development
- Higher queue limit for testing

#### Production (`appsettings.json`)
```json
{
  "RateLimiting": {
    "Enabled": true,
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 2
  }
}
```
- Conservative limits for production
- Lower queue limit to prevent resource exhaustion

### Testing

Run rate limiting tests:

```bash
# All rate limiting tests
dotnet test --filter "FullyQualifiedName~RateLimiting"

# Unit tests only
dotnet test --filter "FullyQualifiedName~RateLimitingOptionsTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~RateLimitingIntegrationTests"

# E2E tests only
dotnet test --filter "FullyQualifiedName~RateLimitingE2eTests"
```

### Disabling Rate Limiting

To disable rate limiting (not recommended for production):

```json
{
  "RateLimiting": {
    "Enabled": false
  }
}
```

Or set via environment variable:
```bash
RateLimiting__Enabled=false
```

## API Endpoints

### Controllers

#### Products (`/api/Product`)
- `GET /api/Product` - Get all products
- `GET /api/Product/{id}` - Get product by ID
- `POST /api/Product` - Create product
- `PUT /api/Product/{id}` - Update product
- `DELETE /api/Product/{id}` - Delete product

#### Orders (`/api/Orders`)
- `POST /api/Orders` - Create order (triggers Dapr workflow)
- `GET /api/Orders/{id}` - Get order status

#### Approvals (`/api/Approvals`)
- `GET /api/Approvals` - Get pending approvals
- `GET /api/Approvals/{orderId}` - Get approval by ID
- `POST /api/Approvals/{orderId}/approve` - Approve order
- `POST /api/Approvals/{orderId}/reject` - Reject order

#### User (`/api/user`)
- `GET /api/user/me/permissions` - Get current user permissions (requires auth)
- `GET /api/user/me/permissions/{permissionName}` - Check permission (requires auth)
- `GET /api/user/roles` - List all roles (Admin only)

#### Weather (`/weather`)
- `GET /weather/{date}` - Get weather forecast
- `DELETE /weather/{date}` - Delete forecast (Admin)

#### Other Endpoints
- `GET /api/TodoItems` - Key Vault demo
- `GET /api/Queue` - Service Bus queue status
- `POST /notify` - Send Service Bus message
- `GET /proxy?url={url}` - HTTP proxy

### Health Checks
- `GET /health` - Health check (all checks must pass)
- `GET /alive` - Liveness check (basic health only)

### API Documentation
- `GET /openapi/v1.json` - OpenAPI specification (dev only)
- `GET /scalar/v1` - Scalar API reference UI (dev only)

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Docker (for local development with Aspire)
- PostgreSQL (via Aspire or standalone)
- Dapr CLI (for workflow support)

### Running Locally

1. **Start the AppHost (Aspire orchestrator)**:
   ```bash
   cd backend/MfePortal.AppHost
   dotnet run
   ```

2. **Or run the API directly**:
   ```bash
   cd backend/AugmentService/AugmentService.Api
   dotnet run
   ```

3. **Access the API**:
   - API: https://localhost:5001
   - Scalar UI: https://localhost:5001/scalar/v1
   - OpenAPI: https://localhost:5001/openapi/v1.json

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test backend/tests/AugmentService/AugmentService.Api.UnitTests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Authentication

### Development Mode

In development, authentication is relaxed:
- Requests without tokens automatically create a test user
- No signature validation
- For testing purposes only

### Production Mode

Production authentication must be configured with Azure AD / Entra ID:
```csharp
builder.Services.AddMicrosoftIdentityWebApiAuthentication(
    builder.Configuration, 
    "AzureAd"
);
```

Add to `appsettings.json`:
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  }
}
```

## Configuration

### Connection Strings

Connection strings are managed by Aspire and injected via environment variables:

- **productdb**: PostgreSQL database for products
- **weatherdb**: PostgreSQL database for weather and users
- **keyvault**: Azure Key Vault
- **messaging**: Azure Service Bus

### Environment Variables

Key environment variables:
- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Production)
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OpenTelemetry endpoint
- `RateLimiting__Enabled` - Enable/disable rate limiting
- `RateLimiting__PermitLimit` - Rate limit threshold

## Monitoring

### OpenTelemetry

The API exports telemetry to OTLP-compatible backends:
- **Logs**: Structured logging with context
- **Metrics**: Request counts, duration, rate limits
- **Traces**: Distributed tracing across services

### Health Checks

Health checks are available at:
- `/health` - All registered health checks
- `/alive` - Liveness check only

These endpoints are **exempt from rate limiting** to ensure monitoring systems can always check service health.

## Architecture

```
AugmentService.Api/
├── Activities/          # Dapr workflow activities
├── Authorization/       # Authorization policies
├── Configuration/       # Configuration classes
├── Controllers/         # MVC controllers
├── Endpoints/           # Minimal API endpoints
├── Middleware/          # Custom middleware
├── Routes/              # Minimal API route groups
├── Workflows/           # Dapr workflows
└── Program.cs           # Application entry point
```

## Contributing

1. Create a feature branch
2. Make changes with tests
3. Run tests: `dotnet test`
4. Submit pull request

## License

[Your License Here]
