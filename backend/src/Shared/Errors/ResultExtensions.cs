using IMS.Modular.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Modular.Shared.Errors;

/// <summary>
/// Extension methods for mapping Result&lt;T&gt; and Result to IResult (Minimal API).
/// Follows RFC 7807 ProblemDetails for error responses with proper HTTP status codes.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a Result&lt;T&gt; to an IResult with the appropriate HTTP status code.
    /// Success returns 200 OK with the value; failure maps ErrorCode to ProblemDetails.
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return ToProblemResult(result.Error, result.ErrorCode, httpContext);
    }

    /// <summary>
    /// Maps a Result&lt;T&gt; to a Created (201) response on success, or ProblemDetails on failure.
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, string location, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
            return Results.Created(location, result.Value);

        return ToProblemResult(result.Error, result.ErrorCode, httpContext);
    }

    /// <summary>
    /// Maps a non-generic Result to NoContent (204) on success, or ProblemDetails on failure.
    /// </summary>
    public static IResult ToApiResult(this Result result, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return ToProblemResult(result.Error, result.ErrorCode, httpContext);
    }

    /// <summary>
    /// Creates a ProblemDetails IResult based on the error code (HTTP status).
    /// Maps: 400 → Bad Request, 401 → Unauthorized, 404 → Not Found, 409 → Conflict, 422 → Unprocessable, 500 → Internal Error.
    /// </summary>
    private static IResult ToProblemResult(string? error, int? errorCode, HttpContext? httpContext)
    {
        var statusCode = errorCode ?? StatusCodes.Status400BadRequest;
        var (title, errorCodeString) = MapStatusCode(statusCode);

        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = error ?? "An error occurred.",
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext?.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCodeString;

        if (correlationId is not null)
            problemDetails.Extensions["correlationId"] = correlationId;

        if (httpContext is not null)
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return Results.Json(problemDetails, statusCode: statusCode, contentType: "application/problem+json");
    }

    private static (string Title, string ErrorCode) MapStatusCode(int statusCode) => statusCode switch
    {
        400 => ("Bad Request", ErrorCodes.ValidationFailed),
        401 => ("Unauthorized", ErrorCodes.Unauthorized),
        403 => ("Forbidden", ErrorCodes.Forbidden),
        404 => ("Not Found", ErrorCodes.ResourceNotFound),
        409 => ("Conflict", ErrorCodes.DuplicateResource),
        422 => ("Unprocessable Entity", ErrorCodes.BusinessRuleViolation),
        500 => ("Internal Server Error", ErrorCodes.InternalError),
        _ => ("Error", ErrorCodes.UnexpectedError)
    };

    /// <summary>
    /// Maps FluentValidation errors into a structured ProblemDetails response (422).
    /// Includes field-level error details in the "errors" extension.
    /// </summary>
    public static IResult ToValidationProblem(
        this IDictionary<string, string[]> validationErrors,
        HttpContext? httpContext = null)
    {
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/422",
            Instance = httpContext?.Request.Path
        };

        problemDetails.Extensions["errorCode"] = ErrorCodes.ValidationFailed;
        problemDetails.Extensions["errors"] = validationErrors;

        if (correlationId is not null)
            problemDetails.Extensions["correlationId"] = correlationId;

        if (httpContext is not null)
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return Results.Json(problemDetails, statusCode: StatusCodes.Status422UnprocessableEntity, contentType: "application/problem+json");
    }
}
