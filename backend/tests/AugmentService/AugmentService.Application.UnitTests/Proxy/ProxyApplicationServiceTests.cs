using Application.Proxy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace AugmentService.Application.UnitTests.Proxy;

public class ProxyApplicationServiceTests
{
    private readonly TestHttpMessageHandler _messageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<ProxyApplicationService>> _loggerMock;
    private readonly ProxyApplicationService _sut;

    public ProxyApplicationServiceTests()
    {
        _messageHandler = new TestHttpMessageHandler();
        _httpClient = new HttpClient(_messageHandler);
        _loggerMock = new Mock<ILogger<ProxyApplicationService>>();
        _sut = new ProxyApplicationService(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_HttpClientIsNull()
    {
        // Act
        var act = () => new ProxyApplicationService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Act
        var act = () => new ProxyApplicationService(_httpClient, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Should_ThrowArgumentNullException_When_TargetUrlIsNull()
    {
        // Act
        var act = async () => await _sut.ProxyRequestAsync(null!, HttpMethod.Get, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_TargetUrlIsEmpty()
    {
        // Act
        var act = async () => await _sut.ProxyRequestAsync("", HttpMethod.Get, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Should_ThrowArgumentException_When_TargetUrlIsWhitespace()
    {
        // Act
        var act = async () => await _sut.ProxyRequestAsync("   ", HttpMethod.Get, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Should_ReturnSuccessResponse_When_ProxyRequestSucceeds()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Success")
        };

        // Act
        var result = await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("Success");
    }

    [Fact]
    public async Task Should_SendGetRequest_When_HttpMethodIsGet()
    {
        // Arrange
        var targetUrl = "https://api.example.com/resource";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        _messageHandler.LastRequest.Should().NotBeNull();
        _messageHandler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        _messageHandler.LastRequest.RequestUri.Should().Be(new Uri(targetUrl));
    }

    [Fact]
    public async Task Should_SendPostRequestWithContent_When_HttpMethodIsPostAndContentProvided()
    {
        // Arrange
        var targetUrl = "https://api.example.com/resource";
        var content = new StringContent("{\"key\":\"value\"}");
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.Created);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Post, content);

        // Assert
        _messageHandler.LastRequest.Should().NotBeNull();
        _messageHandler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        _messageHandler.LastRequest.Content.Should().NotBeNull();
        var capturedContent = await _messageHandler.LastRequest.Content!.ReadAsStringAsync();
        capturedContent.Should().Be("{\"key\":\"value\"}");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task Should_SupportDifferentHttpMethods_When_MethodProvided(string methodName)
    {
        // Arrange
        var targetUrl = "https://api.example.com/resource";
        var method = new HttpMethod(methodName);
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, method, null);

        // Assert
        _messageHandler.LastRequest.Should().NotBeNull();
        _messageHandler.LastRequest!.Method.Should().Be(method);
    }

    [Fact]
    public async Task Should_LogInformationBeforeRequest_When_ProxyRequestCalled()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Proxying") && v.ToString()!.Contains("GET") && v.ToString()!.Contains(targetUrl)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_LogInformationAfterSuccessfulRequest_When_ProxyRequestCompletes()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("completed") && v.ToString()!.Contains(targetUrl)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_LogErrorAndRethrow_When_HttpClientThrowsException()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        var expectedException = new HttpRequestException("Network error");
        _messageHandler.ExceptionToThrow = expectedException;

        // Act
        var act = async () => await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Network error");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error") && v.ToString()!.Contains(targetUrl)),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_RespectCancellationToken_When_TokenProvided()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _messageHandler.ExceptionToThrow = new TaskCanceledException();

        // Act
        var act = async () => await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Should_ReturnErrorResponse_When_TargetReturnsError()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Resource not found")
        };

        // Act
        var result = await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("Resource not found");
    }

    [Fact]
    public async Task Should_HandleNullContent_When_NoContentProvided()
    {
        // Arrange
        var targetUrl = "https://api.example.com/test";
        _messageHandler.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await _sut.ProxyRequestAsync(targetUrl, HttpMethod.Get, null);

        // Assert
        _messageHandler.LastRequest.Should().NotBeNull();
        _messageHandler.LastRequest!.Content.Should().BeNull();
    }
}

// Test helper for HttpMessageHandler
public class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpResponseMessage? ResponseToReturn { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(ResponseToReturn ?? new HttpResponseMessage(HttpStatusCode.OK));
    }
}
