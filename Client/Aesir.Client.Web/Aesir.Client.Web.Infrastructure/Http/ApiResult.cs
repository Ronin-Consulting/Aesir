using System.Net;

namespace Aesir.Client.Web.Infrastructure.Http;

/// <summary>
/// Represents the result of an API operation with success/failure semantics.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
public class ApiResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public HttpStatusCode? StatusCode { get; }

    private ApiResult(bool isSuccess, T? value, string? error, HttpStatusCode? statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static ApiResult<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, value, null, statusCode);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static ApiResult<T> Failure(string error, HttpStatusCode? statusCode = null)
        => new(false, default, error, statusCode);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static ApiResult<T> FromException(Exception ex, HttpStatusCode? statusCode = null)
        => new(false, default, ex.Message, statusCode);

    /// <summary>
    /// Maps the value if successful, otherwise propagates the error.
    /// </summary>
    public ApiResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (IsSuccess && Value is not null)
        {
            return ApiResult<TNew>.Success(mapper(Value), StatusCode ?? HttpStatusCode.OK);
        }
        return ApiResult<TNew>.Failure(Error ?? "Unknown error", StatusCode);
    }

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public ApiResult<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value is not null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public ApiResult<T> OnFailure(Action<string> action)
    {
        if (!IsSuccess && Error is not null)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Gets the value or a default if failed.
    /// </summary>
    public T? GetValueOrDefault(T? defaultValue = default) => IsSuccess ? Value : defaultValue;
}

/// <summary>
/// Represents the result of an API operation without a return value.
/// </summary>
public class ApiResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public HttpStatusCode? StatusCode { get; }

    private ApiResult(bool isSuccess, string? error, HttpStatusCode? statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ApiResult Success(HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(true, null, statusCode);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static ApiResult Failure(string error, HttpStatusCode? statusCode = null)
        => new(false, error, statusCode);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static ApiResult FromException(Exception ex, HttpStatusCode? statusCode = null)
        => new(false, ex.Message, statusCode);

    /// <summary>
    /// Executes an action if successful.
    /// </summary>
    public ApiResult OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Executes an action if failed.
    /// </summary>
    public ApiResult OnFailure(Action<string> action)
    {
        if (!IsSuccess && Error is not null)
        {
            action(Error);
        }
        return this;
    }
}
