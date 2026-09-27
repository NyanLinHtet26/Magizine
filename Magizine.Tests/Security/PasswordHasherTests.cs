using FluentAssertions;
using Magizine.Shared.Security;
using Xunit;

namespace Magizine.Tests.Security;

/// <summary>
/// Permanent regression tests for BCrypt password hashing.
/// Mirrors the deleted Step 6 AuthProbeController verification.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new(); // Default WorkFactor = 12

    [Fact]
    public void Hash_ReturnsValidBcryptHash()
    {
        var hash = _hasher.Hash("correct-horse-battery-staple");

        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().Be(60);
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        var hash = _hasher.Hash("my-secret");
        _hasher.Verify("my-secret", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        var hash = _hasher.Hash("correct");
        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalseForNullOrEmptyPassword()
    {
        var hash = _hasher.Hash("something");
        _hasher.Verify("", hash).Should().BeFalse();
        _hasher.Verify(null!, hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalseForNullOrCorruptHash()
    {
        _hasher.Verify("password", null).Should().BeFalse();
        _hasher.Verify("password", "").Should().BeFalse();
        _hasher.Verify("password", "not-a-hash").Should().BeFalse();
        _hasher.Verify("password", "$2a$12$abc").Should().BeFalse(); // truncated
        _hasher.Verify("password", "plaintext").Should().BeFalse();
    }

    [Fact]
    public void Hash_ThrowsOnEmptyPassword()
    {
        var act = () => _hasher.Hash("");
        act.Should().Throw<ArgumentException>().WithMessage("*required*");
    }

    [Fact]
    public void Hash_ThrowsOnPasswordExceeding72Bytes()
    {
        // 73 bytes in UTF-8
        var longPassword = new string('a', 73);
        var act = () => _hasher.Hash(longPassword);
        act.Should().Throw<ArgumentException>().WithMessage("*72 bytes*");
    }

    [Fact]
    public void Hash_AcceptsExactly72Bytes()
    {
        var exactly72 = new string('b', 72);
        var act = () => _hasher.Hash(exactly72);
        act.Should().NotThrow();
    }

    [Fact]
    public void Verify_ReturnsFalseForOver72BytePassword()
    {
        var hash = _hasher.Hash("short");
        var over72 = new string('c', 73);
        _hasher.Verify(over72, hash).Should().BeFalse(); // cannot match, avoids silent truncation
    }

    [Fact]
    public void NeedsRehash_ReturnsTrueForNullOrBadHash()
    {
        _hasher.NeedsRehash(null).Should().BeTrue();
        _hasher.NeedsRehash("").Should().BeTrue();
        _hasher.NeedsRehash("plaintext").Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_ReturnsTrueWhenCostLowerThanCurrent()
    {
        var lowCostHasher = new PasswordHasher(10);
        var hash = lowCostHasher.Hash("password");
        _hasher.NeedsRehash(hash).Should().BeTrue(); // default is 12
    }

    [Fact]
    public void NeedsRehash_ReturnsFalseWhenCostMatches()
    {
        var hash = _hasher.Hash("password");
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(16)]
    public void Constructor_ThrowsOnWorkFactorOutOfRange(int badFactor)
    {
        var act = () => new PasswordHasher(badFactor);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WorkFactorBounds_AreCorrect()
    {
        PasswordHasher.MinWorkFactor.Should().Be(10);
        PasswordHasher.MaxWorkFactor.Should().Be(15);
        PasswordHasher.DefaultWorkFactor.Should().Be(12);
        PasswordHasher.MaxPasswordBytes.Should().Be(72);
    }
}