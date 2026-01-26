using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace AugmentService.Api.Middleware;

/// <summary>
/// Global exception handler middleware for standardized error responses
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        var errorResponse = new ErrorResponse
        {
            Error = GetErrorType(exception),
            Message = GetUserFriendlyMessage(exception),
            Details = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? exception.ToString()
                : null,
            TraceId = httpContext.TraceIdentifier
        };

        httpContext.Response.StatusCode = GetStatusCode(exception);
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            cancellationToken);

        return true; // Exception handled
    }

    private static string GetErrorType(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "ValidationError",
            UnauthorizedAccessException => "Unauthorized",
            KeyNotFoundException => "NotFound",
            _ => "InternalServerError"
        };
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException argumentException => argumentException.Message,
            UnauthorizedAccessException => "You are not authorized to access this resource.",
            KeyNotFoundException => "The requested resource was not found.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// Standard error response format
    /// </summary>
    private class ErrorResponse
    {
        public required string Error { get; init; }
        public required string Message { get; init; }
        public string? Details { get; init; }
        public required string TraceId { get; init; }
    }
}
