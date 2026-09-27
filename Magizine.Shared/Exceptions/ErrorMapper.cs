using System.Net;
using Magizine.Shared.Enums;
using Magizine.Shared.JsonResources;

namespace Magizine.Shared.Exceptions;

/// <summary>
/// How an exception should be reported to the client: status code, envelope fields, and whether
/// the failure was the caller's fault.
/// </summary>
/// <param name="Status">HTTP status to return.</param>
/// <param name="RespType">Value for <see cref="ErrorEnvelope.RespType"/>.</param>
/// <param name="RespCode">Value for <see cref="ErrorEnvelope.RespCode"/>.</param>
/// <param name="RespDesp">Safe, human-readable summary. Never an exception message.</param>
/// <param name="Errors">Per-field problems, or <c>null</c> when there is nothing useful to map.</param>
/// <param name="IsExpected">
/// True when the failure is part of normal operation and the server is healthy. The handlers use
/// this to log at Warning rather than Error, so a burst of bad input does not look like an
/// incident.
/// </param>
public sealed record ErrorDescriptor(
    HttpStatusCode Status,
    EnumRespType RespType,
    string RespCode,
    string RespDesp,
    IReadOnlyDictionary<string, string[]>? Errors,
    bool IsExpected);

/// <summary>
/// The one place that decides how an exception becomes a response. Shared by all three APIs so the
/// mapping cannot drift between them.
/// </summary>
/// <remarks>
/// <para>
/// Add a case here rather than in an individual <c>GlobalExceptionHandler</c>. When authentication
/// lands, an unauthenticated request should throw (or reuse a future
/// <c>UnauthorizedAccessException</c> case) and be mapped to 401 in exactly one spot.
/// </para>
/// <para>
/// A branch must be added here only for a failure the client can act on. Returning
/// <see cref="HttpStatusCode.InternalServerError"/> for everything unknown is intentional: a
/// stack trace in a 500 is fine for debugging, but an unmapped exception type mapped to a
/// specific status is how a real outage gets reported as a client error.
/// </para>
/// </remarks>
public static class ErrorMapper
{
    /// <summary>Describes how <paramref name="exception"/> should be reported.</summary>
    public static ErrorDescriptor Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // Input the server cannot act on. Expected, and safe to echo field names back.
            ValidationException validation => new ErrorDescriptor(
                HttpStatusCode.BadRequest,
                EnumRespType.Error,
                JsonResource.Validation,
                "One or more fields are invalid.",
                validation.Errors,
                IsExpected: true),

            // Authentication cases are deliberately absent rather than pre-emptively stubbed.
            // Step 6 adds them once there is an actual exception type to map.

            _ => new ErrorDescriptor(
                HttpStatusCode.InternalServerError,
                EnumRespType.SystemError,
                JsonResource.Fail,
                "An unexpected error occurred.",
                Errors: null,
                IsExpected: false)
        };
    }
}
