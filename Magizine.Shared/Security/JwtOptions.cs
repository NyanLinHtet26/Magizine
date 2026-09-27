using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Magizine.Shared.Security;

/// <summary>
/// Bearer-token settings, bound from the <c>Jwt</c> configuration section.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="Issuer"/>, <see cref="Audience"/> and <see cref="AccessTokenExpiryMinutes"/>
/// belong in appsettings.json. <see cref="Key"/> is the signing secret and must come from user
/// secrets or environment variables - it is never committed, and each API is given its own so a
/// token minted by one cannot be presented to another even if issuer checks are ever weakened.
/// </para>
/// <para>
/// <see cref="Validate"/> is called at startup so a missing or weak key fails the host
/// immediately with an actionable message. The alternative - starting up and throwing deep inside
/// the first login request - turns a deployment mistake into a 500 in production.
/// </para>
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length in bytes. HS256 is a 256-bit algorithm, so 32 bytes minimum.</summary>
    public const int MinKeyBytes = 32;

    /// <summary>Bounds on the access-token lifetime, to catch a typo like 60 meaning minutes.</summary>
    public const int MinExpiryMinutes = 1;

    public const int MaxExpiryMinutes = 1440;

    /// <summary>Unique name of the token issuer, checked on every validated request.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Intended recipient of the token, checked on every validated request.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC signing secret. At least <see cref="MinKeyBytes"/> bytes of entropy. Supplied via
    /// user secrets, never committed.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes.</summary>
    [Range(MinExpiryMinutes, MaxExpiryMinutes)]
    public int AccessTokenExpiryMinutes { get; set; } = 60;

    /// <summary>The configured lifetime as a <see cref="TimeSpan"/>.</summary>
    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenExpiryMinutes);

    /// <summary>Wraps <see cref="Key"/> as the HMAC signing key.</summary>
    public SymmetricSecurityKey ToSigningKey() => new(Encoding.UTF8.GetBytes(Key));

    /// <summary>
    /// The token validation rules every authenticated request must satisfy. Lives here, beside
    /// the options, so the Admin and Author APIs cannot drift into different validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TokenValidationParameters.ValidAlgorithms"/> is pinned to HMAC-SHA256. Without
    /// it a token can present a different algorithm - notably <c>none</c> or an asymmetric
    /// algorithm using the HMAC secret as a public key - and pass signature validation.
    /// </para>
    /// <para>
    /// <see cref="TokenValidationParameters.ClockSkew"/> is reduced from the default 5 minutes to
    /// 30 seconds: the default is generous enough to be a meaningful window on a short-lived
    /// token, while 30 seconds only absorbs genuine clock drift between hosts.
    /// </para>
    /// </remarks>
    public TokenValidationParameters ToValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = ToSigningKey(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = MagizineClaims.Subject,
            RoleClaimType = MagizineClaims.Role
        };
    }

    /// <summary>
    /// Throws with setup instructions if anything is missing or unsafe. Called during
    /// registration so a bad configuration prevents startup.
    /// </summary>
    public JwtOptions Validate(string appName)
    {
        var problems = new List<string>();

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            problems.Add($"'{SectionName}:Issuer' is missing.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            problems.Add($"'{SectionName}:Audience' is missing.");
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            problems.Add(
                $"'{SectionName}:Key' is missing. Generate one per API with: "
                + "dotnet user-secrets set \"Jwt:Key\" \"<output>\" --project <path-to-csproj>");
        }
        else
        {
            var keyBytes = Encoding.UTF8.GetByteCount(Key);

            if (keyBytes < MinKeyBytes)
            {
                problems.Add(
                    $"'{SectionName}:Key' is {keyBytes} bytes; at least {MinKeyBytes} are required. "
                    + "A short key makes tokens forgeable.");
            }
        }

        if (AccessTokenExpiryMinutes is < MinExpiryMinutes or > MaxExpiryMinutes)
        {
            problems.Add(
                $"'{SectionName}:AccessTokenExpiryMinutes' is {AccessTokenExpiryMinutes}; "
                + $"it must be between {MinExpiryMinutes} and {MaxExpiryMinutes}.");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"{appName} cannot start with an invalid '{SectionName}' configuration:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, problems.Select(problem => "  - " + problem)));
        }

        return this;
    }
}
