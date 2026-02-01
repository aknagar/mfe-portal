using Azure;
using Azure.Security.KeyVault.Secrets;
using AugmentService.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AugmentService.Api.UnitTests.Controllers;

public class TodoItemsControllerTests
{
    private readonly SecretClient _secretClient;
    private readonly TodoItemsController _controller;

    public TodoItemsControllerTests()
    {
        _secretClient = Substitute.For<SecretClient>();
        _controller = new TodoItemsController(_secretClient);
    }

    [Fact]
    public async Task GetTodoItems_Should_ReturnOk_When_SecretClientIsAvailable()
    {
        // Arrange
        var secretValue = "test-secret-value";
        var secret = CreateKeyVaultSecret("AspireTestSecret", secretValue);
        
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Returns(Response.FromValue(secret, Substitute.For<Response>()));

        // Act
        var result = await _controller.GetTodoItems();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var list = okResult.Value as List<string>;
        list.Should().NotBeNull();
        list.Should().HaveCount(1);
        list![0].Should().Be(secretValue);
    }

    [Fact]
    public async Task GetTodoItems_Should_ReturnBadRequest_When_SecretClientIsNull()
    {
        // Arrange
        var controller = new TodoItemsController(null);

        // Act
        var result = await controller.GetTodoItems();

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result.Result!;
        badRequestResult.Value.Should().Be("SecretClient is not available");
    }

    [Fact]
    public async Task GetTodoItems_Should_CallGetSecretAsync_WithCorrectSecretName()
    {
        // Arrange
        var secret = CreateKeyVaultSecret("AspireTestSecret", "value");
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Returns(Response.FromValue(secret, Substitute.For<Response>()));

        // Act
        await _controller.GetTodoItems();

        // Assert
        await _secretClient.Received(1).GetSecretAsync("AspireTestSecret", null, default);
    }

    [Fact]
    public async Task GetTodoItems_Should_ThrowException_When_SecretNotFound()
    {
        // Arrange
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Throws(new RequestFailedException(404, "Secret not found"));

        // Act
        Func<Task> act = async () => await _controller.GetTodoItems();

        // Assert
        await act.Should().ThrowAsync<RequestFailedException>()
            .WithMessage("*Secret not found*");
    }

    [Fact]
    public async Task GetTodoItems_Should_ThrowException_When_SecretClientThrowsUnauthorized()
    {
        // Arrange
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Throws(new RequestFailedException(401, "Unauthorized"));

        // Act
        Func<Task> act = async () => await _controller.GetTodoItems();

        // Assert
        await act.Should().ThrowAsync<RequestFailedException>()
            .WithMessage("*Unauthorized*");
    }

    [Fact]
    public async Task GetTodoItems_Should_ReturnEmptySecretValue_When_SecretValueIsEmpty()
    {
        // Arrange
        var secret = CreateKeyVaultSecret("AspireTestSecret", "");
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Returns(Response.FromValue(secret, Substitute.For<Response>()));

        // Act
        var result = await _controller.GetTodoItems();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var list = okResult.Value as List<string>;
        list.Should().NotBeNull();
        list.Should().HaveCount(1);
        list![0].Should().BeEmpty();
    }

    [Fact]
    public async Task GetTodoItems_Should_ReturnSecretValue_When_SecretContainsSpecialCharacters()
    {
        // Arrange
        var secretValue = "p@ssw0rd!#$%^&*(){}[]|\\:;\"'<>,.?/~`";
        var secret = CreateKeyVaultSecret("AspireTestSecret", secretValue);
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Returns(Response.FromValue(secret, Substitute.For<Response>()));

        // Act
        var result = await _controller.GetTodoItems();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var list = okResult.Value as List<string>;
        list![0].Should().Be(secretValue);
    }

    [Fact]
    public async Task GetTodoItems_Should_ReturnSecretValue_When_SecretContainsUnicodeCharacters()
    {
        // Arrange
        var secretValue = "Hello 世界 🌍 Café";
        var secret = CreateKeyVaultSecret("AspireTestSecret", secretValue);
        _secretClient.GetSecretAsync("AspireTestSecret", null, default)
            .Returns(Response.FromValue(secret, Substitute.For<Response>()));

        // Act
        var result = await _controller.GetTodoItems();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var list = okResult.Value as List<string>;
        list![0].Should().Be(secretValue);
    }

    /// <summary>
    /// Helper method to create a KeyVaultSecret for testing
    /// </summary>
    private static KeyVaultSecret CreateKeyVaultSecret(string name, string value)
    {
        var secretProperties = SecretModelFactory.SecretProperties(name: name);
        return SecretModelFactory.KeyVaultSecret(secretProperties, value);
    }
}
