using FluentAssertions;
using Magizine.Shared.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace Magizine.Tests.Security;

/// <summary>
/// Permanent regression tests for JWT options validation and token generation.
/// Mirrors the deleted Step 6 auth verification.
/// </summary>
public sealed class JwtTests
{
    [Fact]
    public void JwtOptions_Validate_ThrowsOnMissingIssuer()
    {
        var opts = new JwtOptions { Audience = "aud", Key = new string('k', 64), AccessTokenExpiryMinutes = 60 };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Issuer*missing*");
    }

    [Fact]
    public void JwtOptions_Validate_ThrowsOnMissingAudience()
    {
        var opts = new JwtOptions { Issuer = "iss", Key = new string('k', 64), AccessTokenExpiryMinutes = 60 };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Audience*missing*");
    }

    [Fact]
    public void JwtOptions_Validate_ThrowsOnMissingKey()
    {
        var opts = new JwtOptions { Issuer = "iss", Audience = "aud", AccessTokenExpiryMinutes = 60 };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Key*missing*");
    }

    [Fact]
    public void JwtOptions_Validate_ThrowsOnShortKey()
    {
        var opts = new JwtOptions { Issuer = "iss", Audience = "aud", Key = "short", AccessTokenExpiryMinutes = 60 };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*32*bytes*");
    }

    [Fact]
    public void JwtOptions_Validate_ThrowsOnExpiryOutOfRange()
    {
        var opts = new JwtOptions { Issuer = "iss", Audience = "aud", Key = new string('k', 64), AccessTokenExpiryMinutes = 0 };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccessTokenExpiryMinutes*");

        opts.AccessTokenExpiryMinutes = 1441;
        act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*AccessTokenExpiryMinutes*");
    }

    [Fact]
    public void JwtOptions_Validate_PassesWithValidConfig()
    {
        var opts = new JwtOptions
        {
            Issuer = "https://magizine.test",
            Audience = "magizine-api",
            Key = new string('k', 64),
            AccessTokenExpiryMinutes = 60
        };
        var act = () => opts.Validate("TestApp");
        act.Should().NotThrow();
    }

    [Fact]
    public void JwtOptions_ToValidationParameters_PinsHmacSha256()
    {
        var opts = new JwtOptions
        {
            Issuer = "iss",
            Audience = "aud",
            Key = new string('k', 64),
            AccessTokenExpiryMinutes = 60
        };
        var parameters = opts.ToValidationParameters();

        parameters.ValidateIssuer.Should().BeTrue();
        parameters.ValidIssuer.Should().Be("iss");
        parameters.ValidateAudience.Should().BeTrue();
        parameters.ValidAudience.Should().Be("aud");
        parameters.ValidateIssuerSigningKey.Should().BeTrue();
        parameters.ValidateLifetime.Should().BeTrue();
        parameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(30));
        parameters.RequireExpirationTime.Should().BeTrue();
        parameters.RequireSignedTokens.Should().BeTrue();
        parameters.ValidAlgorithms.Should().ContainSingle(a => a == Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void JwtTokenGenerator_GeneratesValidTokenWithExpectedClaims()
    {
        var opts = new JwtOptions
        {
            Issuer = "iss",
            Audience = "aud",
            Key = new string('k', 64),
            AccessTokenExpiryMinutes = 60
        };

        var generator = new JwtTokenGenerator(Options.Create(opts), TimeProvider.System);
        var token = generator.Generate(42, "adminuser", "SuperAdmin");

        token.Should().NotBeNull();
        token.Token.Should().NotBeNullOrEmpty();
        token.SessionId.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Decode and verify claims (without signature validation for the test)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Token);

        jwt.Issuer.Should().Be("iss");
        jwt.Audiences.Should().Contain("aud");
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == "username" && c.Value == "adminuser");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "SuperAdmin");
        jwt.Claims.Should().Contain(c => c.Type == "typ" && c.Value == "access");
        jwt.Claims.Should().Contain(c => c.Type == "jti" && c.Value == token.SessionId);
    }

    [Fact]
    public void JwtTokenGenerator_ThrowsOnInvalidInputs()
    {
        var opts = new JwtOptions
        {
            Issuer = "iss",
            Audience = "aud",
            Key = new string('k', 64),
            AccessTokenExpiryMinutes = 60
        };

        var generator = new JwtTokenGenerator(Options.Create(opts), TimeProvider.System);

        var act1 = () => generator.Generate(0, "user", "Admin");
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => generator.Generate(1, "", "Admin");
        act2.Should().Throw<ArgumentException>();

        var act3 = () => generator.Generate(1, "user", "");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JwtTokenGenerator_ReusesSessionIdWhenProvided()
    {
        var opts = new JwtOptions
        {
            Issuer = "iss",
            Audience = "aud",
            Key = new string('k', 64),
            AccessTokenExpiryMinutes = 60
        };

        var generator = new JwtTokenGenerator(Options.Create(opts), TimeProvider.System);
        var t1 = generator.Generate(1, "user", "Admin", "fixed-session-id");
        var t2 = generator.Generate(1, "user", "Admin", "fixed-session-id");

        t1.SessionId.Should().Be("fixed-session-id");
        t2.SessionId.Should().Be("fixed-session-id");
    }

    [Fact]
    public void JwtOptions_MinMaxConstants_AreCorrect()
    {
        JwtOptions.MinKeyBytes.Should().Be(32);
        JwtOptions.MinExpiryMinutes.Should().Be(1);
        JwtOptions.MaxExpiryMinutes.Should().Be(1440);
    }
}