using Magizine.Shared.Models.Article;
using Magizine.Shared.Models.Paging;
using Microsoft.AspNetCore.Http;

namespace Magizine.Shared.Models.RequestArticle;

public sealed class RequestArticleResModel
{
    public string RequestArticleId { get; set; } = null!;
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
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedByAdminId { get; set; }
    public string? AdminNotes { get; set; }
    public string? PublishedArticleId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Ordered gallery photos associated with this article request (from dbo.Tbl_ArticlePhoto).
    /// </summary>
    public List<ArticlePhotoResModel> Photos { get; set; } = [];
}

public sealed class RequestArticleDetailReqModel
{
    public string RequestArticleId { get; set; } = null!;
}

public sealed class CreateRequestArticleReqModel
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

    /// <summary>Author can set "Draft" or "Submitted" (or "Submit") upon creation.</summary>
    public string Status { get; set; } = "Draft";
}

public sealed class UpdateRequestArticleReqModel
{
    public string RequestArticleId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Body { get; set; }

    /// <summary>Optional new cover photo file to replace the existing cover photo.</summary>
    public IFormFile? CoverPhoto { get; set; }

    /// <summary>Existing or external cover photo URL if CoverPhoto file is not uploaded.</summary>
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }

    /// <summary>New gallery photos to append to this article request.</summary>
    public List<ArticlePhotoUploadItem>? NewGalleryItems { get; set; }

    /// <summary>26-char ULIDs of existing Tbl_ArticlePhoto rows to soft-delete during update.</summary>
    public List<string>? RemoveArticlePhotoIds { get; set; }

    public string ArticleCategoryId { get; set; } = null!;

    /// <summary>Optional status update ("Draft" or "Submitted"). Editable until Admin marks it "Approved".</summary>
    public string? Status { get; set; }
}

public sealed class ReviewRequestArticleReqModel
{
    public string RequestArticleId { get; set; } = null!;

    /// <summary>"Approved" (auto-publishes to Tbl_Article with Status = 'Published') or "Rejected".</summary>
    public string Status { get; set; } = null!;
    public string? AdminNotes { get; set; }

    /// <summary>When approved, controls whether the published Tbl_Article is marked Featured.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>When approved, controls whether the published Tbl_Article is marked Spotlight.</summary>
    public bool IsSpotlight { get; set; }
}

public sealed class RequestArticleListReqModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchKeyword { get; set; } // By Title or SubTitle
    public string? ArticleCategoryId { get; set; }
    public string? AuthorId { get; set; }
    public string? Status { get; set; }
}
