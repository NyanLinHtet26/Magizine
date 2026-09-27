using FluentAssertions;
using Magizine.Shared.Enums;
using Magizine.Shared.Exceptions;
using Magizine.Shared.JsonResources;
using Xunit;

namespace Magizine.Tests.Exceptions;

/// <summary>
/// Permanent regression tests for validation, error mapping, and the wire envelope.
/// Mirrors the deleted Step 5 validation probes.
/// </summary>
public sealed class ErrorMappingTests
{
    [Fact]
    public void ValidationErrorBuilder_AccumulatesMultipleMessagesPerField()
    {
        var builder = new ValidationErrorBuilder();
        builder.Add("Title", "Required");
        builder.Add("Title", "Too short");

        var dict = builder.ToDictionary();

        dict.Should().NotBeNull();
        dict!["Title"].Should().BeEquivalentTo("Required", "Too short");
    }

    [Fact]
    public void ValidationErrorBuilder_MergesCaseInsensitively()
    {
        var builder = new ValidationErrorBuilder();
        builder.Add("Title", "First");
        builder.Add("title", "Second");

        var dict = builder.ToDictionary();

        dict.Should().NotBeNull();
        dict!["Title"].Should().BeEquivalentTo("First", "Second");
    }

    [Fact]
    public void ValidationErrorBuilder_ReturnsNullWhenEmpty()
    {
        var builder = new ValidationErrorBuilder();
        builder.ToDictionary().Should().BeNull();
    }

    [Fact]
    public void ValidationErrorBuilder_FieldCount_ReflectsDistinctFields()
    {
        var builder = new ValidationErrorBuilder();
        builder.Add("A", "1");
        builder.Add("B", "2");
        builder.Add("a", "3");

        builder.FieldCount.Should().Be(2);
    }

    [Fact]
    public void ValidationErrorBuilder_ThrowIfInvalid_ThrowsWithCollectedErrors()
    {
        var builder = new ValidationErrorBuilder();
        builder.Add("Email", "Invalid format");

        var act = () => builder.ThrowIfInvalid();

        act.Should().Throw<ValidationException>()
            .Which.Errors.Should().ContainKey("Email")
            .WhoseValue.Should().Contain("Invalid format");
    }

    [Fact]
    public void ValidationException_Constructor_SingleMessage()
    {
        var ex = new ValidationException("Slug", "Already taken");

        ex.Errors.Should().ContainKey("Slug");
        ex.Errors["Slug"].Should().BeEquivalentTo("Already taken");
    }

    [Fact]
    public void ValidationException_Constructor_MultipleMessages()
    {
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = ["1", "2"],
            ["B"] = ["3"]
        };
        var ex = new ValidationException(dict);

        ex.Errors.Should().BeEquivalentTo(dict);
    }

    [Fact]
    public void ValidationException_ThrowsOnEmptyDictionary()
    {
        var act = () => new ValidationException(new Dictionary<string, string[]>());
        act.Should().Throw<ArgumentException>().WithMessage("*at least one error*");
    }

    [Fact]
    public void ErrorMapper_Describe_ValidationException_MapsTo400()
    {
        var ex = new ValidationException("Field", "Bad");
        var desc = ErrorMapper.Describe(ex);

        desc.Status.Should().Be(System.Net.HttpStatusCode.BadRequest);
        desc.RespType.Should().Be(EnumRespType.Error);
        desc.RespCode.Should().Be(JsonResource.Validation);
        desc.IsExpected.Should().BeTrue();
        desc.Errors.Should().NotBeNull();
    }

    [Fact]
    public void ErrorMapper_Describe_AuthorizationException_MapsTo403()
    {
        var ex = new AuthorizationException("Nope");
        var desc = ErrorMapper.Describe(ex);

        desc.Status.Should().Be(System.Net.HttpStatusCode.Forbidden);
        desc.RespType.Should().Be(EnumRespType.Error);
        desc.RespCode.Should().Be(JsonResource.Forbidden);
        desc.IsExpected.Should().BeTrue();
        desc.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorMapper_Describe_UnauthorizedAccessException_MapsTo401()
    {
        var ex = new UnauthorizedAccessException();
        var desc = ErrorMapper.Describe(ex);

        desc.Status.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        desc.RespType.Should().Be(EnumRespType.Error);
        desc.RespCode.Should().Be(JsonResource.Unauthorized);
        desc.IsExpected.Should().BeTrue();
        desc.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorMapper_Describe_UnknownException_MapsTo500()
    {
        var ex = new InvalidOperationException("boom");
        var desc = ErrorMapper.Describe(ex);

        desc.Status.Should().Be(System.Net.HttpStatusCode.InternalServerError);
        desc.RespType.Should().Be(EnumRespType.SystemError);
        desc.RespCode.Should().Be(JsonResource.Fail);
        desc.IsExpected.Should().BeFalse();
        desc.Errors.Should().BeNull();
    }

    [Fact]
    public void ErrorEnvelope_HasRequiredProperties()
    {
        var env = new ErrorEnvelope
        {
            RespType = EnumRespType.Error,
            RespCode = JsonResource.Validation,
            RespDesp = "Invalid input",
            TraceId = "abc123",
            Errors = new Dictionary<string, string[]>
            {
                ["Field"] = ["Bad"]
            }
        };

        env.RespType.Should().Be(EnumRespType.Error);
        env.RespCode.Should().Be(JsonResource.Validation);
        env.Errors.Should().NotBeNull();
    }
}