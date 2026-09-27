using System.Diagnostics;
using Magizine.Shared.JsonResources;
using Microsoft.AspNetCore.Diagnostics;

namespace MagizineAdmin.Api;

/// <summary>
/// Last-resort handler for exceptions that escape a service. Logs the full detail and returns
/// only a generic message, so nothing internal reaches a client.
/// </summary>
/// <remarks>
/// Complements <c>Result&lt;T&gt;.Error(ex, ...)</c>, which deliberately discards
/// <c>ex.Message</c> and routes the exception to the logger through its callback. Anything that
/// arrives here was thrown rather than wrapped, so it is unexpected by definition.
/// </remarks>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled {ExceptionType} on {Method} {Path} (traceId {TraceId})",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // Shaped like Result<T> so clients parse one envelope. No exception detail, by design.
        // RespType is the enum *name* to match JsonStringEnumConverter in Program.cs, and the
        // property names are PascalCase to match Result<T>'s own serialised casing exactly.
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                RespType = nameof(EnumRespType.SystemError),
                RespCode = JsonResource.Fail,
                RespDesp = "An unexpected error occurred.",
                TraceId = traceId
            },
            cancellationToken);

        return true;
    }
}
