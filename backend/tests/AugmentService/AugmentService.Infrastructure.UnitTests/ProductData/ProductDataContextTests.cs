using AugmentService.Infrastructure;
using AugmentService.Infrastructure.ProductData;
using AugmentService.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AugmentService.Infrastructure.UnitTests.ProductData;

public class ProductDataContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ProductDataContext _context;

    public ProductDataContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ProductDataContext>()
            .UseSqlite(_connection)
            .Options;

        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = "Data Source=:memory:",
            EnableSensitiveDataLogging = false
        });

        _context = new ProductDataContext(options, config);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void ProductDataContext_Should_CreateSchema_WithProductTable()
    {
        // Assert - if EnsureCreated succeeded the schema exists
        _context.Product.Should().NotBeNull();
    }

    [Fact]
    public async Task ProductDataContext_OnModelCreating_Should_ConfigureProductEntity()
    {
        // Arrange - insert a product to exercise model configuration
        var product = new Product
        {
            Name = "Test Product",
            Description = "Test description",
            Price = 29.99m,
            ImageUrl = "test.png"
        };

        // Act
        _context.Product.Add(product);
        await _context.SaveChangesAsync();

        // Assert - model mapping is exercised when we read back
        var saved = await _context.Product.FirstOrDefaultAsync(p => p.Name == "Test Product");
        saved.Should().NotBeNull();
        saved!.Price.Should().Be(29.99m);
        saved.Id.Should().BeGreaterThan(0); // ValueGeneratedOnAdd works
    }

    [Fact]
    public void ProductDataContext_OnConfiguring_Should_NotReconfigure_When_AlreadyConfigured()
    {
        // The context is already configured via constructor options (UseSqlite).
        // OnConfiguring checks optionsBuilder.IsConfigured and skips the Npgsql branch.
        // We verify no exception is thrown when using the context normally.
        var act = () => _context.Product.ToList();
        act.Should().NotThrow();
    }
}

public class ProductDbInitializerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ProductDataContext _context;

    public ProductDbInitializerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ProductDataContext>()
            .UseSqlite(_connection)
            .Options;

        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = "Data Source=:memory:",
            EnableSensitiveDataLogging = false
        });

        _context = new ProductDataContext(options, config);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void Initialize_Should_SeedProducts_When_TableIsEmpty()
    {
        // Act
        DbInitializer.Initialize(_context);

        // Assert - seed data has 9 products
        var count = _context.Product.Count();
        count.Should().Be(9);
    }

    [Fact]
    public void Initialize_Should_NotSeedProducts_When_TableAlreadyHasData()
    {
        // Arrange - add a product so the table is non-empty
        _context.Product.Add(new Product
        {
            Name = "Existing",
            Description = "Existing product",
            Price = 1m,
            ImageUrl = "existing.png"
        });
        _context.SaveChanges();

        // Act
        DbInitializer.Initialize(_context);

        // Assert - only our one product remains (no seed data added)
        var count = _context.Product.Count();
        count.Should().Be(1);
    }
}

public class ProductExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ProductExtensionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void CreateProductDbIfNotExists_Should_CreateSchemaAndSeedData()
    {
        // Arrange - build a minimal host that registers ProductDataContext
        var options = new DbContextOptionsBuilder<ProductDataContext>()
            .UseSqlite(_connection)
            .Options;

        var config = Options.Create(new InfrastructureConfig
        {
            ConnectionString = "Data Source=:memory:",
            EnableSensitiveDataLogging = false
        });

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(config);
        builder.Services.AddDbContext<ProductDataContext>(opt =>
            opt.UseSqlite(_connection));

        var app = builder.Build();

        // Act
        app.CreateProductDbIfNotExists();

        // Assert - seed data should exist after call
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDataContext>();
        context.Product.Count().Should().BeGreaterThan(0);
    }
}
