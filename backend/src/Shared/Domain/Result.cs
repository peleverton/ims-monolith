namespace IMS.Modular.Shared.Domain;

/// <summary>
/// Result pattern for Railway-Oriented Programming
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int? ErrorCode { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string error, int errorCode = 400)
    {
        IsSuccess = false;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error, int errorCode = 400) => new(error, errorCode);
    public static Result<T> NotFound(string error) => new(error, 404);
    public static Result<T> Unauthorized(string error) => new(error, 401);
    public static Result<T> Forbidden(string error) => new(error, 403);
    public static Result<T> Conflict(string error) => new(error, 409);
    public static Result<T> Unprocessable(string error) => new(error, 422);
}

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int? ErrorCode { get; }

    private Result(bool success) => IsSuccess = success;
    private Result(string error, int errorCode = 400)
    {
        IsSuccess = false;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error, int errorCode = 400) => new(error, errorCode);
    public static Result NotFound(string error) => new(error, 404);
    public static Result Forbidden(string error) => new(error, 403);
    public static Result Conflict(string error) => new(error, 409);
}
