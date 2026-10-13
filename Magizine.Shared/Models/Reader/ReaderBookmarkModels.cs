using System.ComponentModel.DataAnnotations;
using Magizine.Shared.Models.Article;

namespace Magizine.Shared.Models.Reader;

public sealed class BookmarkArticleReqModel
{
    [Required(ErrorMessage = "ArticleId is required.")]
    [StringLength(26, MinimumLength = 26, ErrorMessage = "ArticleId must be a valid 26-character identifier.")]
    public string ArticleId { get; set; } = null!;
}

public sealed class BookmarkToggleResModel
{
    public string ArticleId { get; set; } = null!;
    public bool IsBookmarked { get; set; }
    public int BookmarkCount { get; set; }
    public string Message { get; set; } = null!;
}

public sealed class BookmarkCheckResModel
{
    public string ArticleId { get; set; } = null!;
    public bool IsBookmarked { get; set; }
}

public sealed class BookmarkListReqModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    [StringLength(100)]
    public string? SearchKeyword { get; set; }

    [StringLength(26)]
    public string? ArticleCategoryId { get; set; }
}

public sealed class BookmarkedArticleResModel
{
    public string ReaderBookmarkId { get; set; } = null!;
    public string ArticleId { get; set; } = null!;
    public DateTimeOffset BookmarkedAt { get; set; }
    public ArticleResModel Article { get; set; } = null!;
}
