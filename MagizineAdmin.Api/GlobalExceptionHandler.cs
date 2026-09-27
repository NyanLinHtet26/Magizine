using System.Diagnostics;
using Magizine.Shared.Enums;
using Magizine.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace MagizineAdmin.Api;

/// <summary>
/// Last-resort handler for exceptions that escape a service. Maps the failure through the shared
/// <see cref="ErrorMapper"/>, logs the full detail, and returns only what is safe for a client.
/// </summary>
/// <remarks>
/// <para>
/// Two paths, deliberately distinguished:
/// a <see cref="ValidationException"/> becomes HTTP 400 with per-field messages at Warning level,
/// because the caller's input was wrong and the server is fine; anything else becomes HTTP 500 at
/// Error level with no detail, because the server has a problem the client cannot help with.
/// </para>
/// <para>
/// Complements <c>Result&lt;T&gt;.Error(ex, ...)</c>, which deliberately discards
/// <c>ex.Message</c> and routes the exception to the logger through its callback. Anything that
/// arrives here was thrown rather than wrapped, so it is unexpected by definition - except for
/// <see cref="ValidationException"/>, which is thrown on purpose.
/// </para>
/// <para>
/// The response shape comes from <see cref="ErrorEnvelope"/> and the status/field mapping from
/// <see cref="ErrorMapper"/>, both shared with the other two APIs so they cannot diverge. This
/// file carries no mapping decisions of its own.
/// </para>
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var descriptor = ErrorMapper.Describe(exception);

        if (descriptor.IsExpected)
        {
            // Warning, not Error: the server is healthy and this input will keep happening.
            logger.LogWarning(
                "{ExceptionType} on {Method} {Path} (traceId {TraceId}); fields: {Fields}",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId,
                descriptor.Errors is null
                    ? "-"
                    : string.Join(", ", descriptor.Errors.Keys));
        }
        else
        {
            // The full exception goes to the log, never to the response.
            logger.LogError(
                exception,
                "Unhandled {ExceptionType} on {Method} {Path} (traceId {TraceId})",
                exception.GetType().Name,
                httpContext.Request.Method,
                httpContext.Request.Path,
                traceId);
        }

        httpContext.Response.StatusCode = (int)descriptor.Status;
        httpContext.Response.ContentType = "application/json";

        // Shaped like Result<T> so clients parse one envelope. RespType is the enum name via
        // JsonStringEnumConverter, and property names are PascalCase via PropertyNamingPolicy = null.
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorEnvelope
            {
                RespType = descriptor.RespType,
                RespCode = descriptor.RespCode,
                RespDesp = descriptor.RespDesp,
                TraceId = traceId,
                Errors = descriptor.Errors
            },
            cancellationToken);

        return true;
    }
}
