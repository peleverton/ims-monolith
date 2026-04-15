namespace IMS.Modular.Shared.Errors;

/// <summary>
/// Centralized error code catalog for consistent API error responses.
/// Each category maps to a specific HTTP status code range.
/// </summary>
public static class ErrorCodes
{
    // ── Validation (400 Bad Request) ────────────────────────────────
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string InvalidInput = "INVALID_INPUT";
    public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";
    public const string InvalidFormat = "INVALID_FORMAT";

    // ── Authentication (401 Unauthorized) ───────────────────────────
    public const string Unauthorized = "UNAUTHORIZED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string TokenExpired = "TOKEN_EXPIRED";

    // ── Authorization (403 Forbidden) ───────────────────────────────
    public const string Forbidden = "FORBIDDEN";
    public const string InsufficientPermissions = "INSUFFICIENT_PERMISSIONS";

    // ── Not Found (404) ─────────────────────────────────────────────
    public const string NotFound = "NOT_FOUND";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";

    // ── Conflict (409) ──────────────────────────────────────────────
    public const string Conflict = "CONFLICT";
    public const string DuplicateResource = "DUPLICATE_RESOURCE";

    // ── Business Validation (422 Unprocessable Entity) ──────────────
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";

    // ── Internal (500 Internal Server Error) ────────────────────────
    public const string InternalError = "INTERNAL_ERROR";
    public const string UnexpectedError = "UNEXPECTED_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}
