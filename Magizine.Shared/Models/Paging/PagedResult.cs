namespace Magizine.Shared.Models.Paging;

/// <summary>
/// Paged result envelope. Wraps the items plus metadata for rendering pagination controls.
/// </summary>
/// <remarks>
/// <para>
/// <c>TotalCount</c> and <c>TotalPages</c> are 0 when the query returns no rows.
/// <c>HasPrevious</c> / <c>HasNext</c> are derived from <c>Page</c> vs <c>TotalPages</c>.
/// </para>
/// <para>
/// The generic <c>T</c> is typically a DTO projection (never an entity). The consumer
/// wraps this in <c>Result<PagedResult<T>></c> so the HTTP layer sees one
/// uniform envelope.
/// </para>
/// </remarks>
public sealed class PagedResult<T>
{
    /// <summary>The items for the current page. Empty when no rows match.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>1-based page number of this result.</summary>
    public int Page { get; init; }

    /// <summary>Page size used for this result.</summary>
    public int PageSize { get; init; }

    /// <summary>Total rows matching the (unpaged) query.</summary>
    public long TotalCount { get; init; }

    /// <summary>Total pages = ceil(TotalCount / PageSize). 0 when TotalCount = 0.</summary>
    public int TotalPages { get; init; }

    /// <summary>True when <c>Page > 1</c>.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>True when <c>Page < TotalPages</c>.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>Factory for an empty page (TotalCount = 0).</summary>
    public static PagedResult<T> Empty(int page, int pageSize)
        => new PagedResult<T>
        {
            Items = [],
            Page = page,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 0
        };

    /// <summary>Factory from a counted query result.</summary>
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        long totalCount,
        PageRequest request)
    {
        var totalPages = request.PageSize > 0
            ? (int)((totalCount + request.PageSize - 1) / request.PageSize)
            : 0;

        return new PagedResult<T>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}