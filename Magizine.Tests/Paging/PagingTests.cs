using FluentAssertions;
using Magizine.Shared.Models.Paging;
using Xunit;

namespace Magizine.Tests.Paging;

/// <summary>
/// Permanent regression tests for the shared pagination models.
/// </summary>
public sealed class PagingTests
{
    [Fact]
    public void PageRequest_Create_DefaultsToPage1Size20()
    {
        var req = PageRequest.Create(null, null);

        req.Page.Should().Be(PageRequest.DefaultPage);
        req.PageSize.Should().Be(PageRequest.DefaultPageSize);
        req.Offset.Should().Be(0);
    }

    [Fact]
    public void PageRequest_Create_ClampsPageBelow1()
    {
        var req = PageRequest.Create(0, 10);
        req.Page.Should().Be(1);
        req.Offset.Should().Be(0);

        req = PageRequest.Create(-5, 10);
        req.Page.Should().Be(1);
        req.Offset.Should().Be(0);
    }

    [Fact]
    public void PageRequest_Create_ClampsPageSizeBelow1()
    {
        var req = PageRequest.Create(2, 0);
        req.PageSize.Should().Be(PageRequest.DefaultPageSize);
    }

    [Fact]
    public void PageRequest_Create_ClampsPageSizeAboveMax()
    {
        var req = PageRequest.Create(1, 500);
        req.PageSize.Should().Be(PageRequest.MaxPageSize);
    }

    [Fact]
    public void PageRequest_Create_ComputesOffsetCorrectly()
    {
        PageRequest.Create(1, 20).Offset.Should().Be(0);
        PageRequest.Create(2, 20).Offset.Should().Be(20);
        PageRequest.Create(3, 10).Offset.Should().Be(20);
    }

    [Fact]
    public void PageRequest_Constants_AreCorrect()
    {
        PageRequest.DefaultPage.Should().Be(1);
        PageRequest.DefaultPageSize.Should().Be(20);
        PageRequest.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void PagedResult_Empty_FactoryProducesZeroMetadata()
    {
        var result = PagedResult<string>.Empty(3, 10);

        result.Items.Should().BeEmpty();
        result.Page.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_Create_ComputesTotalPagesCorrectly()
    {
        var req = PageRequest.Create(1, 10);
        var items = new List<string> { "a", "b", "c", "d", "e" };

        var result = PagedResult<string>.Create(items, 25, req);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3); // ceil(25/10)
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Create_LastPage_HasNextFalse()
    {
        var req = PageRequest.Create(3, 10);
        var items = new List<string> { "y", "z" };

        var result = PagedResult<string>.Create(items, 25, req);

        result.Page.Should().Be(3);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_Create_ZeroTotalCount_TotalPagesZero()
    {
        var req = PageRequest.Create(1, 10);
        var result = PagedResult<string>.Create([], 0, req);

        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
    }
}