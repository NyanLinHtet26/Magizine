using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Magizine.Shared.Security;

/// <summary>A freshly minted access token and the metadata a client needs to use it.</summary>
/// <param name="Token">The signed JWT. Never logged.</param>
/// <param name="ExpiresAt">When the token stops being accepted, for the client's own countdown.</param>
/// <param name="SessionId">The <c>jti</c> embedded in the token.</param>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt, string SessionId);

/// <summary>A freshly generated opaque refresh token and its SHA-256 database hash.</summary>
/// <param name="Token">The 64-byte CSPRNG Base64Url string sent in the HttpOnly cookie. Never stored in plaintext.</param>
/// <param name="TokenHash">The uppercase SHA-256 hex digest stored in <c>Tbl_Admin.RefreshTokenHash</c>.</param>
/// <param name="ExpiresAt">When the refresh token expires.</param>
public sealed record RefreshToken(string Token, string TokenHash, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues signed access tokens and cryptographically random refresh tokens.
/// </summary>
/// <remarks>
/// <para>
/// Access tokens are stateless JWTs (<c>sub</c>, <c>jti</c>, <c>username</c>, <c>role</c>, <c>typ=access</c>).
/// Refresh tokens are 64-byte CSPRNG opaque tokens whose SHA-256 hash is persisted in the database
/// to support rotation, a 30-second concurrent grace period, and server-side revocation on logout.
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
    public AccessToken Generate(string userId, string userName, string role, string? sessionId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.AccessTokenLifetime);
        var jti = string.IsNullOrWhiteSpace(sessionId)
            ? Ulid.NewUlid().ToString()
            : sessionId;

        var claims = new List<Claim>
        {
            new(MagizineClaims.Subject, userId),
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

    /// <summary>
    /// Generates a 64-byte CSPRNG refresh token and its SHA-256 hex digest.
    /// </summary>
    public RefreshToken GenerateRefreshToken()
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.RefreshTokenLifetime);
        var rawBytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Base64UrlEncoder.Encode(rawBytes);
        var tokenHash = ComputeTokenHash(rawToken);

        return new RefreshToken(rawToken, tokenHash, expiresAt);
    }

    /// <summary>
    /// Computes the deterministic uppercase SHA-256 hex hash of a raw refresh token.
    /// </summary>
    public string ComputeTokenHash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(hashBytes);
    }
}
