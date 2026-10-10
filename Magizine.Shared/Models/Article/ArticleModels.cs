using Magizine.Shared.Models.Paging;
using Microsoft.AspNetCore.Http;

namespace Magizine.Shared.Models.Article;

/// <summary>
/// Atomic unit for uploading a single gallery photo paired with its metadata.
/// Bound via FormData keys: GalleryItems[0].File, GalleryItems[0].Caption, GalleryItems[0].Credit, GalleryItems[0].SortOrder.
/// Eliminates parallel-array index mismatch bugs.
/// </summary>
public sealed class ArticlePhotoUploadItem
{
    public IFormFile File { get; set; } = null!;
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ArticlePhotoResModel
{
    public string ArticlePhotoId { get; set; } = null!;
    public string? ArticleId { get; set; }
    public string? RequestArticleId { get; set; }
    public string PhotoUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ArticleResModel
{
    public string ArticleId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string Slug { get; set; } = null!;
    public string? Body { get; set; }
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }
    
    public string ArticleCategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;

    public string AuthorId { get; set; } = null!;
    public string AuthorName { get; set; } = null!;

    public string Status { get; set; } = null!;
    public DateTimeOffset? PublishedAt { get; set; }
    
    public bool IsFeatured { get; set; }
    public bool IsSpotlight { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Ordered gallery photos associated with this article (from dbo.Tbl_ArticlePhoto).
    /// </summary>
    public List<ArticlePhotoResModel> Photos { get; set; } = [];
}

public sealed class ArticleDetailReqModel
{
    public string ArticleId { get; set; } = null!;
}

public sealed class CreateArticleReqModel
{
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Body { get; set; }

    /// <summary>Optional cover photo file uploaded directly via multipart/form-data (max 5 MB).</summary>
    public IFormFile? CoverPhoto { get; set; }

    /// <summary>Fallback or external cover photo URL if no CoverPhoto file is uploaded.</summary>
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }

    /// <summary>Structured gallery photo items (max 10 items per request).</summary>
    public List<ArticlePhotoUploadItem>? GalleryItems { get; set; }
    
    public string ArticleCategoryId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    
    // Status can be set to "Draft" or "Published" upon creation.
    public string Status { get; set; } = "Draft"; 
    public bool IsFeatured { get; set; }
    public bool IsSpotlight { get; set; }
}

public sealed class UpdateArticleReqModel
{
    public string ArticleId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Body { get; set; }

    /// <summary>Optional new cover photo file to replace the existing cover photo.</summary>
    public IFormFile? CoverPhoto { get; set; }

    /// <summary>Existing or external cover photo URL if CoverPhoto file is not uploaded.</summary>
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }

    /// <summary>New gallery photos to append to this article.</summary>
    public List<ArticlePhotoUploadItem>? NewGalleryItems { get; set; }

    /// <summary>26-char ULIDs of existing Tbl_ArticlePhoto rows to soft-delete during update.</summary>
    public List<string>? RemoveArticlePhotoIds { get; set; }
    
    public string ArticleCategoryId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
}

public sealed class ChangeArticleStatusReqModel
{
    public string ArticleId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool IsFeatured { get; set; }
    public bool IsSpotlight { get; set; }
}

public sealed class ArticleListReqModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchKeyword { get; set; } // By Title or SubTitle
    public string? ArticleCategoryId { get; set; }
    public string? AuthorId { get; set; }
    public string? Status { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsSpotlight { get; set; }
}

public sealed class PurgeExpiredPhotosReqModel
{
    /// <summary>
    /// Number of days to retain soft-deleted photos before permanently deleting their physical files (default: 30).
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}
