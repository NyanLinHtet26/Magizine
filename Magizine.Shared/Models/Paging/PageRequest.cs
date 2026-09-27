namespace Magizine.Shared.Models.Paging;

/// <summary>
/// Normalized paging request. Deserialized from query string; never trusted as-is.
/// </summary>
/// <remarks>
/// <para>
/// Defaults: Page=1, PageSize=20. Maximum PageSize is 100. Both are clamped on
/// construction so no caller can bypass the limit by sending absurd values.
/// </para>
/// <para>
/// Used as a flat type in query-string binding (no nested object). The controller
/// receives <c>int page, int pageSize</c> and builds this record.
/// </para>
/// </remarks>
public sealed record PageRequest
{
    /// <summary>Default page number (1-based).</summary>
    public const int DefaultPage = 1;

    /// <summary>Default page size.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Hard ceiling on page size to protect the database and response size.</summary>
    public const int MaxPageSize = 100;

    /// <summary>1-based page number. Always >= 1.</summary>
    public int Page { get; init; }

    /// <summary>Items per page. Always in [1, MaxPageSize].</summary>
    public int PageSize { get; init; }

    /// <summary>Creates a normalized request from raw values. Clamps to valid ranges.</summary>
    public static PageRequest Create(int? page, int? pageSize)
    {
        var p = page.GetValueOrDefault(DefaultPage);
        var ps = pageSize.GetValueOrDefault(DefaultPageSize);

        if (p < 1) p = DefaultPage;
        if (ps < 1) ps = DefaultPageSize;
        if (ps > MaxPageSize) ps = MaxPageSize;

        return new PageRequest { Page = p, PageSize = ps };
    }

    /// <summary>Zero-based offset for SQL <c>OFFSET</c> / <c>Skip</c>.</summary>
    public int Offset => (Page - 1) * PageSize;
}