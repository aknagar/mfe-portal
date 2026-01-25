---
name: dotnet-test-generator
description: Set up comprehensive .NET testing framework with xUnit, NSubstitute, TestContainers, and Aspire.Hosting.Testing for distributed app testing. Use when setting up test projects, creating unit/integration tests, or testing Aspire applications.
license: MIT
metadata:
  audience: .net-developers
  framework: xunit
  tools: nsubstitute, testcontainers, coverlet, aspire
---

# .NET Test Generator

I set up complete testing frameworks for .NET applications including Aspire distributed apps.

## When to Use Me

- Setting up test infrastructure for .NET projects
- Creating test projects (unit, integration, E2E)
- Testing .NET Aspire distributed applications
- Adding test utilities and fixtures
- Configuring code coverage

## Test Project Structure

```
tests/
├── {ProjectName}.UnitTests/
│   ├── Domain/
│   │   └── EntityTests.cs
│   ├── Application/
│   │   └── ServiceTests.cs
│   └── Infrastructure/
│       └── RepositoryTests.cs
├── {ProjectName}.IntegrationTests/
│   ├── Api/
│   │   └── EndpointTests.cs
│   └── Database/
│       └── DbContextTests.cs
└── {ProjectName}.AppHost.Tests/        # Aspire distributed app tests
    └── IntegrationTests.cs

Common/
├── TestDataBuilders/
├── Fakes/
└── TestHelpers.cs
```

## Package References

### Standard Test Project
```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
<PackageReference Include="NSubstitute" Version="5.3.0" />
<PackageReference Include="NSubstitute.Analyzers.CSharp" Version="1.0.17">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
<PackageReference Include="coverlet.collector" Version="6.0.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

### Aspire Distributed App Test Project
```xml
<PackageReference Include="Aspire.Hosting.Testing" Version="9.1.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
```

### Integration Tests with TestContainers
```xml
<PackageReference Include="Testcontainers" Version="4.3.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.3.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
```

## Unit Test Template with NSubstitute

```csharp
using NSubstitute;
using FluentAssertions;
using AutoFixture;
using AutoFixture.Xunit2;

namespace MyProject.UnitTests;

public class ProductServiceTests
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;
    private readonly ProductService _sut;
    private readonly Fixture _fixture;

    public ProductServiceTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _logger = Substitute.For<ILogger<ProductService>>();
        _sut = new ProductService(_repository, _logger);
        _fixture = new Fixture();
    }

    [Fact]
    public async Task GetProductById_WhenExists_ReturnsProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expected = _fixture.Build<Product>()
            .With(p => p.Id, productId)
            .Create();
        
        _repository.GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expected);
        await _repository.Received(1).GetByIdAsync(productId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateProduct_WhenNameInvalid_ThrowsValidationException(string? name)
    {
        // Arrange
        var request = new CreateProductRequest { Name = name!, Price = 10 };

        // Act
        var act = () => _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Name*required*");
    }

    [Theory, AutoData]
    public async Task UpdateProduct_WhenValid_UpdatesAndReturns(Product product)
    {
        // Arrange
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _repository.UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(product);

        // Act
        var result = await _sut.UpdateAsync(product.Id, new UpdateRequest { Name = "New" });

        // Assert
        result.Should().NotBeNull();
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Product>(p => p.Name == "New"),
            Arg.Any<CancellationToken>());
    }
}
```

## NSubstitute Quick Reference

```csharp
// Create substitute
var service = Substitute.For<IMyService>();

// Setup returns
service.GetAsync(Arg.Any<int>()).Returns(Task.FromResult(value));
service.GetAsync(123).Returns(value);                    // Specific arg
service.GetAsync(Arg.Is<int>(x => x > 0)).Returns(v);   // Conditional

// Verify calls
await service.Received().GetAsync(123);
await service.Received(2).GetAsync(Arg.Any<int>());
await service.DidNotReceive().DeleteAsync(Arg.Any<int>());

// Capture arguments
var capturedArgs = new List<int>();
service.ProcessAsync(Arg.Do<int>(x => capturedArgs.Add(x)));

// Throw exceptions
service.GetAsync(Arg.Any<int>()).ThrowsAsync(new Exception());

// Multiple returns
service.GetAsync(Arg.Any<int>()).Returns(x => count++);
```

## Aspire Distributed Application Tests

### Basic AppHost Test
```csharp
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyApp.AppHost.Tests;

public class IntegrationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task AppHost_StartsSuccessfully()
    {
        // Arrange
        var cts = new CancellationTokenSource(DefaultTimeout);
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>(cts.Token);

        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        await using var app = await appHost.BuildAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);
        
        await app.StartAsync(cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);

        // Act
        using var httpClient = app.CreateHttpClient("webfrontend");
        await app.ResourceNotifications.WaitForResourceHealthyAsync(
            "webfrontend", cts.Token)
            .WaitAsync(DefaultTimeout, cts.Token);
        
        using var response = await httpClient.GetAsync("/", cts.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### Test Resource Environment Variables
```csharp
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

public class EnvVarTests
{
    [Fact]
    public async Task WebResource_HasCorrectApiServiceBinding()
    {
        // Arrange
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>();

        var frontend = builder.CreateResourceBuilder<ProjectResource>("webfrontend");

        // Act
        var envVars = await frontend.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish);

        // Assert
        Assert.Contains(envVars, kvp =>
            kvp.Key == "APISERVICE_HTTPS" &&
            kvp.Value == "{apiservice.bindings.https.url}");
    }
}
```

### Aspire Test with xUnit Output Logging
```csharp
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

public class IntegrationTestsWithLogging : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private DistributedApplication? _app;

    public IntegrationTestsWithLogging(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>();

        appHost.Services.AddLogging(logging =>
        {
            logging.AddXUnit(_output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Warning);
        });

        _app = await appHost.BuildAsync();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ApiEndpoint_ReturnsData()
    {
        var client = _app!.CreateHttpClient("apiservice");
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice");
        
        var response = await client.GetAsync("/api/products");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Integration Tests with TestContainers

```csharp
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add test database
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetProducts_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/products");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Test Data Builders

```csharp
public class ProductBuilder
{
    private readonly Fixture _fixture = new();
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Product";
    private decimal _price = 9.99m;

    public ProductBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public Product Build() => new()
    {
        Id = _id,
        Name = _name,
        Price = _price
    };

    public static implicit operator Product(ProductBuilder b) => b.Build();
}

// Usage
var product = new ProductBuilder()
    .WithName("Widget")
    .WithPrice(19.99m)
    .Build();
```

## Code Coverage Configuration

Add to test project `.csproj`:
```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <ExcludeByAttribute>GeneratedCodeAttribute,ExcludeFromCodeCoverageAttribute</ExcludeByAttribute>
  <ExcludeByFile>**/Migrations/**</ExcludeByFile>
</PropertyGroup>
```

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

## Project Template Commands

```bash
# Create xUnit test project for Aspire
dotnet new aspire-xunit -o MyApp.AppHost.Tests
cd MyApp.AppHost.Tests
dotnet add reference ../MyApp.AppHost/MyApp.AppHost.csproj

# Create standard xUnit test project
dotnet new xunit -o MyApp.UnitTests
cd MyApp.UnitTests
dotnet add package NSubstitute
dotnet add package FluentAssertions
dotnet add package AutoFixture.Xunit2
```

## Best Practices

1. **Test behavior, not implementation** - Focus on what the code does, not how
2. **Use descriptive test names** - `Method_Scenario_ExpectedResult`
3. **Follow AAA pattern** - Arrange, Act, Assert
4. **One assertion concept per test** - Keep tests focused
5. **Use NSubstitute for dependencies** - Clean syntax, good diagnostics
6. **Use FluentAssertions for readability** - Expressive assertion messages
7. **Use AutoFixture for test data** - Reduce boilerplate
8. **Use TestContainers for integration** - Real dependencies in isolation
9. **Use Aspire.Hosting.Testing for distributed apps** - Test the full stack
10. **Aim for >80% meaningful coverage** - Quality over quantity
