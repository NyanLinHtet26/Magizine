namespace Magizine.Shared.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but not permitted to perform the action. Mapped to
/// HTTP 403, as distinct from <see cref="UnauthorizedAccessException"/> which means "we do not
/// know who you are".
/// </summary>
/// <remarks>
/// For the common case - a role check - prefer the declarative
/// <c>[Authorize(Roles = nameof(EnumAdminRole.SuperAdmin))]</c> attribute, which the framework
/// turns into a 403 with no code in the service. Throw this from a service when the rule depends
/// on data that is not a role, for example "only the author of this article may edit it", or when
/// the failure needs a specific <c>RespCode</c> the client can branch on.
/// </remarks>
public sealed class AuthorizationException : Exception
{
    /// <summary>Client-facing explanation. Must not describe why the rule exists.</summary>
    public string? RespDesp { get; }

    public AuthorizationException(string? respDesp = null, Exception? innerException = null)
        : base(respDesp ?? "You are not allowed to perform this action.", innerException)
    {
        RespDesp = respDesp;
    }
}
