# MfePortal Clean Architecture

This document describes the Clean Architecture implementation for the MfePortal backend.

## Architecture Overview

MfePortal follows the **Clean Architecture** pattern with clear separation of concerns across multiple layers:

```
┌─────────────────────────────────────────┐
│      AugmentService (API/Presentation)  │
│         (Minimal APIs, Controllers)     │
└────────────────────┬────────────────────┘
                     │ depends on
                     ▼
┌─────────────────────────────────────────┐
│    MfePortal.Application (Use Cases)    │
│  (Business Logic, Services, DTOs)       │
└────────────────────┬────────────────────┘
                     │ depends on
                     ▼
┌─────────────────────────────────────────┐
│   MfePortal.Core (Domain & Contracts)   │
│  (Entities, Interfaces, Exceptions)     │
└─────────────────────────────────────────┘
                     ▲
                     │ implements
                     │
┌─────────────────────────────────────────┐
│  MfePortal.Infrastructure (Persistence) │
│ (Repositories, External Services, EF)   │
└─────────────────────────────────────────┘
```

## Layer Responsibilities

### 1. **Core Layer** (`MfePortal.Core`)
The innermost layer containing pure domain logic with zero external dependencies.

**Contents:**
- **Entities**: Domain models (`BaseEntity`, `ProxyTarget`)
- **Interfaces**: Abstractions for repositories and services
- **Exceptions**: Domain-specific exceptions
- **ValueObjects**: Immutable value types for domain concepts

**Key Characteristics:**
- No external package dependencies (except standard libraries)
- Independent and reusable
- Contains business rules and validation logic

**Example:**
```csharp
public class ProxyTarget : BaseEntity
{
    public required string Name { get; set; }
    public required string BaseUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}
```

---

### 2. **Application Layer** (`MfePortal.Application`)
Contains application-specific business logic, orchestrates use cases, and defines service contracts.

**Contents:**
- **Services**: Application services that implement use cases (e.g., `ProxyApplicationService`)
- **Interfaces**: Service contracts (e.g., `IProxyService`)
- **DTOs**: Data Transfer Objects for API contracts
- **Validators**: Input validation logic
- **Mappers**: Object mapping between entities and DTOs

**Key Characteristics:**
- Depends only on the Core layer
- Contains the main business logic
- Framework-agnostic (can be tested without web framework)
- Orchestrates domain entities

**Example:**
```csharp
public class ProxyApplicationService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyApplicationService> _logger;

    public async Task<HttpResponseMessage> ProxyRequestAsync(
        string targetUrl, 
        HttpMethod method, 
        HttpContent? content, 
        CancellationToken cancellationToken = default)
    {
        // Business logic here
    }
}
```

---

### 3. **Infrastructure Layer** (`MfePortal.Infrastructure`)
Handles all external concerns: databases, APIs, configuration, etc.

**Contents:**
- **Repositories**: Implementations of Core interfaces (e.g., `InMemoryProxyTargetRepository`)
- **Services**: External service integrations
- **Configuration**: DbContext, service registration
- **Extensions**: Dependency injection setup (`InfrastructureServiceExtensions`)

**Key Characteristics:**
- Depends on Core and Application layers
- Contains concrete implementations of abstractions
- Handles all I/O operations
- Can be replaced/upgraded without affecting other layers

**Example:**
```csharp
public class InMemoryProxyTargetRepository : IProxyTargetRepository
{
    private readonly Dictionary<Guid, ProxyTarget> _targets = [];

    public async Task<ProxyTarget> AddAsync(ProxyTarget target, CancellationToken cancellationToken = default)
    {
        _targets[target.Id] = target;
        return target;
    }
}
```

---

### 4. **API Layer** (`AugmentService`)
The presentation layer that exposes endpoints to clients.

**Contents:**
- **Program.cs**: Service configuration and pipeline setup
- **Endpoints**: Minimal APIs or Controllers
- **Models**: Request/Response models
- **Middleware**: Cross-cutting concerns (logging, error handling)

**Key Characteristics:**
- Depends on all other layers
- HTTP protocol handling
- Framework-specific (ASP.NET Core)
- Orchestrates Application services for client requests

**Example:**
```csharp
app.MapGet("/proxy", async (string url, IProxyService proxyService) =>
{
    var response = await proxyService.ProxyRequestAsync(url, HttpMethod.Get, null);
    return Results.Stream(...);
})
.WithOpenApi();
```

---

## Dependency Flow

**Key Rule**: Dependencies flow INWARD toward the Core.

```
AugmentService (API)
    ↓ depends on
MfePortal.Application
    ↓ depends on
MfePortal.Core
    ↑ implemented by
MfePortal.Infrastructure
```

### What This Means:

✅ **Allowed:**
- Infrastructure implements Core interfaces
- Application uses Core entities
- API injects Application services

❌ **Not Allowed:**
- Core imports from Application
- Application imports from Infrastructure
- Core knows about database details

---

## Service Registration Example

All services are registered in `InfrastructureServiceExtensions`:

```csharp
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories
        services.AddSingleton<IProxyTargetRepository, InMemoryProxyTargetRepository>();

        // Application Services
        services.AddScoped<IProxyService, ProxyApplicationService>();

        // HTTP Client
        services.AddHttpClient<IProxyService, ProxyApplicationService>();

        return services;
    }
}
```

Used in `AugmentService/Program.cs`:
```csharp
builder.Services.AddInfrastructureServices();
```

---

## Adding New Features

### Step 1: Define the Domain (Core)
```csharp
// MfePortal.Core/Entities/NewEntity.cs
public class NewEntity : BaseEntity
{
    public required string Name { get; set; }
}

// MfePortal.Core/Interfaces/INewEntityRepository.cs
public interface INewEntityRepository
{
    Task<NewEntity> AddAsync(NewEntity entity);
    Task<NewEntity?> GetByIdAsync(Guid id);
}
```

### Step 2: Define Use Cases (Application)
```csharp
// MfePortal.Application/Interfaces/INewEntityService.cs
public interface INewEntityService
{
    Task<NewEntity> CreateAsync(string name);
    Task<NewEntity?> GetAsync(Guid id);
}

// MfePortal.Application/Services/NewEntityService.cs
public class NewEntityService : INewEntityService
{
    private readonly INewEntityRepository _repository;

    public async Task<NewEntity> CreateAsync(string name)
    {
        var entity = new NewEntity { Name = name };
        return await _repository.AddAsync(entity);
    }
}
```

### Step 3: Implement Infrastructure (Infrastructure)
```csharp
// MfePortal.Infrastructure/Repositories/NewEntityRepository.cs
public class NewEntityRepository : INewEntityRepository
{
    public async Task<NewEntity> AddAsync(NewEntity entity)
    {
        // Implementation
    }
}

// Register in InfrastructureServiceExtensions
services.AddScoped<INewEntityRepository, NewEntityRepository>();
services.AddScoped<INewEntityService, NewEntityService>();
```

### Step 4: Expose via API (AugmentService)
```csharp
// In Program.cs
app.MapPost("/entities", async (string name, INewEntityService service) =>
{
    var entity = await service.CreateAsync(name);
    return Results.Created($"/entities/{entity.Id}", entity);
})
.WithOpenApi();
```

---

## Testing Strategy

Clean Architecture enables easy testing at each layer:

### Unit Tests (Core + Application)
```csharp
[Fact]
public async Task ProxyApplicationService_Should_Log_Request()
{
    // Arrange
    var mockHttpClient = new Mock<HttpClient>();
    var mockLogger = new Mock<ILogger<ProxyApplicationService>>();
    var service = new ProxyApplicationService(mockHttpClient.Object, mockLogger.Object);

    // Act
    await service.ProxyRequestAsync("https://example.com", HttpMethod.Get, null);

    // Assert
    mockLogger.Verify(x => x.Log(...), Times.Once);
}
```

### Integration Tests (Infrastructure)
```csharp
[Fact]
public async Task InMemoryRepository_Should_Store_And_Retrieve_Entities()
{
    // Arrange
    var logger = new Mock<ILogger<InMemoryProxyTargetRepository>>();
    var repository = new InMemoryProxyTargetRepository(logger.Object);
    var entity = new ProxyTarget { Name = "Test", BaseUrl = "http://test" };

    // Act
    var created = await repository.AddAsync(entity);
    var retrieved = await repository.GetByIdAsync(created.Id);

    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal("Test", retrieved.Name);
}
```

---

## Migration Path: From In-Memory to Entity Framework

The architecture supports seamless upgrades:

### Current State (In-Memory)
```csharp
services.AddSingleton<IProxyTargetRepository, InMemoryProxyTargetRepository>();
```

### Future State (With Database)
```csharp
services.AddDbContext<MfePortalDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
services.AddScoped<IProxyTargetRepository, EfProxyTargetRepository>();
```

No changes needed in Application or API layers!

---

## Best Practices

1. **Keep Core Independent**: Never add external dependencies to Core layer
2. **Use Interfaces**: Always program to abstractions, not implementations
3. **Single Responsibility**: Each service has one reason to change
4. **Dependency Injection**: Let the framework inject dependencies
5. **Logging**: Add logging at service boundaries
6. **Validation**: Validate inputs at API layer, business rules in Application layer
7. **Error Handling**: Use domain exceptions, catch and map in API layer

---

## Further Reading

- [Uncle Bob's Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Microsoft Docs: Architecture Principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)

---

**Architecture Last Updated**: December 31, 2025  
**Aspire Integration**: ✅ Configured (see MfePortal.AppHost)  
**Dapr Integration**: ✅ Ready (via Application layer services)
