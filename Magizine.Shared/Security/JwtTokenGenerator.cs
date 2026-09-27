using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Magizine.Shared.Security;

/// <summary>A freshly minted access token and the metadata a client needs to use it.</summary>
/// <param name="Token">The signed JWT. Never logged.</param>
/// <param name="ExpiresAt">When the token stops being accepted, for the client's own countdown.</param>
/// <param name="SessionId">The <c>jti</c> embedded in the token.</param>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt, string SessionId);

/// <summary>
/// Issues signed access tokens. The only place in the solution that creates a JWT.
/// </summary>
/// <remarks>
/// <para>
/// Tokens are stateless: the <c>sub</c> claim carries the user id and nothing is stored server
/// side. That is a deliberate trade - no session table to query on every request, but also no
/// instant revocation. When logout-that-actually-logs-out or forced sign-out is needed, add a
/// security-version claim and check it against the user row, or move to reference tokens.
/// </para>
/// <para>
/// The identity in the token is the real user id, not an encrypted blob and not a shared
/// "subject" config value. Nothing is encrypted here: the payload is readable by anyone holding
/// the token, which is fine because it contains no secrets - only an id, a username and a role.
/// Signatures stop tampering, not reading.
/// </para>
/// <para>
/// Uses <see cref="TimeProvider"/> rather than <see cref="DateTimeOffset.UtcNow"/> so token
/// lifetimes are testable without waiting.
/// </para>
/// <para>
/// Stateless and thread-safe; register as a singleton.
/// </para>
/// </remarks>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Creates a signed token for the given user.
    /// </summary>
    /// <param name="userId">Stored as the <c>sub</c> claim; the caller's identity.</param>
    /// <param name="userName">Stored for display and log correlation only.</param>
    /// <param name="role">
    /// Role name matching an <c>EnumAdminRole</c> member for admins, or the author role name.
    /// </param>
    /// <param name="sessionId">Existing session to reuse; a new id is generated when omitted.</param>
    public AccessToken Generate(long userId, string userName, string role, string? sessionId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.AccessTokenLifetime);
        var jti = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")
            : sessionId;

        var claims = new List<Claim>
        {
            new(MagizineClaims.Subject, userId.ToString(CultureInfo.InvariantCulture)),
            new(MagizineClaims.SessionId, jti),
            new(MagizineClaims.UserName, userName),
            new(MagizineClaims.Role, role),
            new(MagizineClaims.TokenType, MagizineClaims.AccessTokenType),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(now.UtcDateTime).ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                _options.ToSigningKey(),
                SecurityAlgorithms.HmacSha256));

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            jti);
    }
}
