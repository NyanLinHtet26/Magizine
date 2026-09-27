namespace Magizine.Shared.Security;

/// <summary>
/// Claim type names used in our bearer tokens.
/// </summary>
/// <remarks>
/// Deliberately the raw RFC 7519 short names, not <c>ClaimTypes.*</c>. The JwtBearer handler is
/// configured with <c>MapInboundClaims = false</c>, so a claim named <c>role</c> in the token
/// arrives as <c>role</c> in <c>HttpContext.User</c> - no silent rewrite to
/// <see cref="System.Security.Claims.ClaimTypes.Role"/>. The brief
/// <c>NameClaimType</c> and <c>RoleClaimType</c> are set to match these constants, so
/// <c>[Authorize]</c> and <c>[Authorize(Roles = ...)]</c> keep working.
/// </remarks>
public static class MagizineClaims
{
    /// <summary>Subject - the numeric user id, as a string. The stable identity of the caller.</summary>
    public const string Subject = "sub";

    /// <summary>JWT id - a per-login identifier, so a specific session can be traced or revoked.</summary>
    public const string SessionId = "jti";

    /// <summary>Role name, matching an <c>EnumAdminRole</c> member. Drives role-based authorization.</summary>
    public const string Role = "role";

    /// <summary>Display username. Never used for authorization - only for display and logging.</summary>
    public const string UserName = "username";

    /// <summary>
    /// Token purpose: <c>access</c> for API calls. Present so a future refresh token cannot be
    /// replayed as an access token.
    /// </summary>
    public const string TokenType = "typ";

    /// <summary>Value of <see cref="TokenType"/> for a normal API token.</summary>
    public const string AccessTokenType = "access";
}
