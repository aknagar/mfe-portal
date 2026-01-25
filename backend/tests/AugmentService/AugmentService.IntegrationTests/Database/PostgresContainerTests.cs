using FluentAssertions;
using Testcontainers.PostgreSql;
using Xunit;

namespace AugmentService.IntegrationTests.Database;

public class PostgresContainerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ConnectionString_IsNotEmpty()
    {
        // Act
        var connectionString = _postgres.GetConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Host=");
        connectionString.Should().Contain("Database=");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Database_CanExecuteQuery()
    {
        // Arrange
        await using var connection = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        // Act
        await using var command = new Npgsql.NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().Be(1);
    }
}
