using FluentAssertions;
using Magizine.Shared.Security;
using Xunit;

namespace Magizine.Tests.Security;

/// <summary>
/// Permanent regression tests for CORS validation.
/// Mirrors the deleted Step 7 CORS verification.
/// </summary>
public sealed class CorsOptionsTests
{
    [Fact]
    public void CorsOptions_EmptyAllowlist_IsEmpty()
    {
        var opts = new CorsOptions { AllowedOrigins = [] };
        opts.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void CorsOptions_Validate_PassesWithValidOrigins()
    {
        var opts = new CorsOptions
        {
            AllowedOrigins = ["https://app.example.com", "https://admin.example.com:8443"]
        };
        var act = () => opts.Validate("TestApp");
        act.Should().NotThrow();
    }

    [Fact]
    public void CorsOptions_Validate_ThrowsOnBlankEntry()
    {
        var opts = new CorsOptions { AllowedOrigins = ["https://app.example.com", ""] };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*blank entry*");
    }

    [Fact]
    public void CorsOptions_Validate_ThrowsOnWildcard()
    {
        var opts = new CorsOptions { AllowedOrigins = ["https://*.example.com"] };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*wildcard*");
    }

    [Fact]
    public void CorsOptions_Validate_ThrowsOnTrailingSlash()
    {
        var opts = new CorsOptions { AllowedOrigins = ["https://app.example.com/"] };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*trailing slash*");
    }

    [Fact]
    public void CorsOptions_Validate_ThrowsOnMalformedOrigin()
    {
        var opts = new CorsOptions { AllowedOrigins = ["not-a-url"] };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*not an absolute http(s) origin*");
    }

    [Fact]
    public void CorsOptions_Validate_ThrowsOnFtpScheme()
    {
        var opts = new CorsOptions { AllowedOrigins = ["ftp://files.example.com"] };
        var act = () => opts.Validate("TestApp");
        act.Should().Throw<InvalidOperationException>().WithMessage("*not an absolute http(s) origin*");
    }

    [Fact]
    public void CorsOptions_Validate_ReturnsSelfForChaining()
    {
        var opts = new CorsOptions { AllowedOrigins = ["https://app.example.com"] };
        var result = opts.Validate("TestApp");
        result.Should().BeSameAs(opts);
    }
}