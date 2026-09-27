using FluentAssertions;
using Magizine.Shared.Enums;
using Xunit;

namespace Magizine.Tests.Enums;

/// <summary>
/// Permanent regression tests for the typed database-backed enums and their extensions.
/// Mirrors the deleted Step 4 validation probes.
/// </summary>
public sealed class EnumsTests
{
    [Fact]
    public void EnumArticleStatus_HasExpectedMembers()
    {
        Enum.GetNames<EnumArticleStatus>().Should().BeEquivalentTo("Draft", "InReview", "Published", "Archived");
        Enum.GetValues<EnumArticleStatus>().Should().HaveCount(4);
    }

    [Fact]
    public void EnumRequestArticleStatus_HasExpectedMembers()
    {
        Enum.GetNames<EnumRequestArticleStatus>().Should().BeEquivalentTo("Pending", "UnderReview", "Accepted", "Rejected");
        Enum.GetValues<EnumRequestArticleStatus>().Should().HaveCount(4);
    }

    [Fact]
    public void EnumAdminRole_HasExpectedMembers()
    {
        Enum.GetNames<EnumAdminRole>().Should().BeEquivalentTo("SuperAdmin", "Admin", "Editor");
        Enum.GetValues<EnumAdminRole>().Should().HaveCount(3);
    }

    [Fact]
    public void EnumNotificationType_HasExpectedMembers()
    {
        Enum.GetNames<EnumNotificationType>().Should().BeEquivalentTo("Article", "RequestArticle", "ContactMessage", "NewsletterSubscription", "Author", "System");
        Enum.GetValues<EnumNotificationType>().Should().HaveCount(6);
    }

    [Fact]
    public void EnumAdPlacement_HasExpectedMembers()
    {
        Enum.GetNames<EnumAdPlacement>().Should().BeEquivalentTo("Header", "Sidebar", "Footer", "InArticleTop", "InArticleBottom", "BetweenArticles", "Popup");
        Enum.GetValues<EnumAdPlacement>().Should().HaveCount(7);
    }

    [Fact]
    public void EnumRelatedEntityType_HasExpectedMembers()
    {
        Enum.GetNames<EnumRelatedEntityType>().Should().BeEquivalentTo("Article", "RequestArticle", "ContactMessage", "Author", "Admin", "ArticleCategory", "Newsletter", "Ad");
        Enum.GetValues<EnumRelatedEntityType>().Should().HaveCount(8);
    }

    [Fact]
    public void TotalEnumMemberCount_Is32()
    {
        var total = Enum.GetNames<EnumArticleStatus>().Length
            + Enum.GetNames<EnumRequestArticleStatus>().Length
            + Enum.GetNames<EnumAdminRole>().Length
            + Enum.GetNames<EnumNotificationType>().Length
            + Enum.GetNames<EnumAdPlacement>().Length
            + Enum.GetNames<EnumRelatedEntityType>().Length;

        total.Should().Be(32);
    }

    [Fact]
    public void LongestMemberName_IsNewsletterSubscription_22Chars()
    {
        var allNames = Enum.GetNames<EnumArticleStatus>()
            .Concat(Enum.GetNames<EnumRequestArticleStatus>())
            .Concat(Enum.GetNames<EnumAdminRole>())
            .Concat(Enum.GetNames<EnumNotificationType>())
            .Concat(Enum.GetNames<EnumAdPlacement>())
            .Concat(Enum.GetNames<EnumRelatedEntityType>());

        var longest = allNames.MaxBy(n => n.Length);
        longest.Should().Be("NewsletterSubscription");
        longest!.Length.Should().Be(22);
    }

    [Theory]
    [InlineData("Draft", EnumArticleStatus.Draft)]
    [InlineData("InReview", EnumArticleStatus.InReview)]
    [InlineData("Published", EnumArticleStatus.Published)]
    [InlineData("Archived", EnumArticleStatus.Archived)]
    [InlineData("draft", EnumArticleStatus.Draft)]
    [InlineData("INREVIEW", EnumArticleStatus.InReview)]
    [InlineData("published", EnumArticleStatus.Published)]
    public void EnumExtensions_ToEnum_ParsesCaseInsensitive(string input, EnumArticleStatus expected)
    {
        input.ToEnum<EnumArticleStatus>().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void EnumExtensions_ToEnum_ThrowsOnNullOrEmpty(string? input)
    {
        var act = () => input!.ToEnum<EnumArticleStatus>();
        act.Should().Throw<FormatException>().WithMessage("*null or empty*");
    }

    [Fact]
    public void EnumExtensions_ToEnum_ThrowsOnUnknownValue()
    {
        var act = () => "NotAValue".ToEnum<EnumArticleStatus>();
        act.Should().Throw<FormatException>().WithMessage("*'NotAValue' is not a valid EnumArticleStatus*");
    }

    [Fact]
    public void EnumExtensions_ToEnumOrDefault_ReturnsFallbackOnUnknown()
    {
        "Unknown".ToEnumOrDefault(EnumArticleStatus.Draft).Should().Be(EnumArticleStatus.Draft);
    }

    [Fact]
    public void EnumExtensions_TryToEnum_ReturnsTrueOnKnown()
    {
        "Published".TryToEnum(out EnumArticleStatus result).Should().BeTrue();
        result.Should().Be(EnumArticleStatus.Published);
    }

    [Fact]
    public void EnumExtensions_TryToEnum_ReturnsFalseOnUnknown()
    {
        "Unknown".TryToEnum(out EnumArticleStatus result).Should().BeFalse();
        result.Should().Be(default);
    }

    [Fact]
    public void EnumExtensions_ToDbString_RoundTrips()
    {
        EnumArticleStatus.Published.ToDbString().Should().Be("Published");
        "Published".ToEnum<EnumArticleStatus>().ToDbString().Should().Be("Published");
    }

    [Fact]
    public void AllEnums_RoundTripAllMembers()
    {
        foreach (var name in Enum.GetNames<EnumArticleStatus>())
            name.ToEnum<EnumArticleStatus>().ToDbString().Should().Be(name);

        foreach (var name in Enum.GetNames<EnumRequestArticleStatus>())
            name.ToEnum<EnumRequestArticleStatus>().ToDbString().Should().Be(name);

        foreach (var name in Enum.GetNames<EnumAdminRole>())
            name.ToEnum<EnumAdminRole>().ToDbString().Should().Be(name);

        foreach (var name in Enum.GetNames<EnumNotificationType>())
            name.ToEnum<EnumNotificationType>().ToDbString().Should().Be(name);

        foreach (var name in Enum.GetNames<EnumAdPlacement>())
            name.ToEnum<EnumAdPlacement>().ToDbString().Should().Be(name);

        foreach (var name in Enum.GetNames<EnumRelatedEntityType>())
            name.ToEnum<EnumRelatedEntityType>().ToDbString().Should().Be(name);
    }
}