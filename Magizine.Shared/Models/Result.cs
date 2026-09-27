using Magizine.Shared.Enums;

namespace Magizine.Shared.Models;

/// <summary>
/// Transport-agnostic result of a service call. Intended for *expected* failures
/// (validation, not found, conflict) - not for exceptional ones.
/// </summary>
/// <remarks>
/// Deliberately has no dependency on ASP.NET Core, so the same type is usable from a
/// console app, worker, or test. Converting a result to an HTTP response is the API
/// layer's job.
/// </remarks>
public class Result<T>
{
    private const string DefaultSuccessCode = "MS#000";
    private const string DefaultErrorCode = "ME#999";

    public bool IsSuccess => RespType == EnumRespType.Success;

    /// <summary>
    /// True only for genuine failures. <see cref="EnumRespType.None"/> and
    /// <see cref="EnumRespType.Warning"/> are deliberately excluded - note this is not
    /// simply "not success", which would flag a default-constructed result as an error.
    /// </summary>
    public bool IsError => RespType is EnumRespType.Error or EnumRespType.SystemError;

    public EnumRespType RespType { get; set; } = EnumRespType.None;

    /// <summary>Message code for the caller or client to switch on, e.g. "MS#000".</summary>
    public string RespCode { get; set; } = string.Empty;

    /// <summary>Human-readable description. Never populated from an exception message.</summary>
    public string? RespDesp { get; set; }

    /// <summary>Values for interpolating a client-side message template.</summary>
    public object[]? RespDespParameter { get; set; }

    public T? Data { get; set; }

    public static Result<T> Success(T data, string code = DefaultSuccessCode)
    {
        return new Result<T> { Data = data, RespCode = code, RespType = EnumRespType.Success };
    }

    public static Result<T> Success(string code = DefaultSuccessCode)
    {
        return new Result<T> { RespCode = code, RespType = EnumRespType.Success };
    }

    public static Result<T> Error(T data, string code)
    {
        return new Result<T> { Data = data, RespCode = code, RespType = EnumRespType.Error };
    }

    public static Result<T> Error(string code)
    {
        return new Result<T> { RespCode = code, RespType = EnumRespType.Error };
    }

    /// <summary>
    /// Wraps an exception as a system error. The exception message is deliberately
    /// discarded so internal detail (connection strings, SQL, column names) cannot reach a
    /// client; pass <paramref name="onError"/> to log it instead.
    /// </summary>
    /// <example>
    /// <code>Result&lt;Article&gt;.Error(ex, "ME#999", logger.LogError);</code>
    /// </example>
    public static Result<T> Error(
        Exception ex,
        string code = DefaultErrorCode,
        Action<Exception>? onError = null)
    {
        onError?.Invoke(ex);

        return new Result<T>
        {
            RespDesp = "An unexpected error occurred.",
            RespCode = code,
            RespType = EnumRespType.SystemError
        };
    }

    public static Result<T> Error(string code = DefaultErrorCode, string? respDesp = null)
    {
        return new Result<T> { RespDesp = respDesp, RespCode = code, RespType = EnumRespType.Error };
    }

    public static Result<T> DataError(string messageCode, T data, string? message = null)
    {
        return new Result<T> { RespDesp = message, RespType = EnumRespType.Error, Data = data, RespCode = messageCode };
    }

    public static Result<T> Error(string code, params object[] parameters)
    {
        return new Result<T>
        {
            RespCode = code,
            RespType = EnumRespType.Error,
            RespDespParameter = parameters
        };
    }

    public static Result<T> Success(string code, params object[] parameters)
    {
        return new Result<T>
        {
            RespCode = code,
            RespType = EnumRespType.Success,
            RespDespParameter = parameters
        };
    }

    public static Result<T> Success(string code, T data, params object[] parameters)
    {
        return new Result<T>
        {
            RespCode = code,
            RespType = EnumRespType.Success,
            RespDespParameter = parameters,
            Data = data
        };
    }
}
