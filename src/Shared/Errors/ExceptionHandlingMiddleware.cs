using IMS.Modular.Shared.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IMS.Modular.Shared.Errors;

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions and returns RFC 7807 ProblemDetails
/// with correlation ID enrichment. Stack traces are only included in Development.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? "N/A";

        // Log the full exception with correlation context
        _logger.LogError(exception,
            "Unhandled exception caught by ExceptionHandlingMiddleware. " +
            "CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId, context.Request.Path, context.Request.Method);

        var (statusCode, errorCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // Always include correlation ID and error code
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Include stack trace only in Development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = new
            {
                message = exception.Message,
                type = exception.GetType().FullName,
                stackTrace = exception.StackTrace?.Split(Environment.NewLine)
            };
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions));
    }

    private static (int StatusCode, string ErrorCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            // Validation / argument errors → 400
            ArgumentException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.InvalidInput,
                "Bad Request",
                ex.Message),

            FormatException ex => (
                StatusCodes.Status400BadRequest,
                ErrorCodes.InvalidFormat,
                "Bad Request",
                ex.Message),

            // Auth errors → 401
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ErrorCodes.Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource."),

            // Not found → 404
            KeyNotFoundException ex => (
                StatusCodes.Status404NotFound,
                ErrorCodes.ResourceNotFound,
                "Not Found",
                ex.Message),

            // Conflict → 409
            InvalidOperationException ex when ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                ErrorCodes.DuplicateResource,
                "Conflict",
                ex.Message),

            // Invalid state → 422
            InvalidOperationException ex => (
                StatusCodes.Status422UnprocessableEntity,
                ErrorCodes.BusinessRuleViolation,
                "Unprocessable Entity",
                ex.Message),

            // Timeout → 504
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                ErrorCodes.ServiceUnavailable,
                "Gateway Timeout",
                "The request timed out. Please try again later."),

            // Task cancelled → 499 (Client Closed Request)
            OperationCanceledException => (
                499, // Non-standard but widely used
                "REQUEST_CANCELLED",
                "Client Closed Request",
                "The request was cancelled by the client."),

            // Everything else → 500
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.UnexpectedError,
                "Internal Server Error",
                "An unexpected error occurred. Please contact support if the problem persists.")
        };
    }
}
