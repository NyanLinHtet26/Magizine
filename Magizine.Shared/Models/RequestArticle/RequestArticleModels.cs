using Magizine.Shared.Models.Paging;

namespace Magizine.Shared.Models.RequestArticle;

public sealed class RequestArticleResModel
{
    public long RequestArticleId { get; set; }
    public string PitcherName { get; set; } = null!;
    public string PitcherEmail { get; set; } = null!;
    public string? ProposedTitle { get; set; }
    public string PitchBody { get; set; } = null!;
    public long? ArticleCategoryId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByAdminId { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class RequestArticleDetailReqModel
{
    public long RequestArticleId { get; set; }
}

public sealed class CreateRequestArticleReqModel
{
    public string PitcherName { get; set; } = null!;
    public string PitcherEmail { get; set; } = null!;
    public string? ProposedTitle { get; set; }
    public string PitchBody { get; set; } = null!;
    public long? ArticleCategoryId { get; set; }
}

public sealed class ReviewRequestArticleReqModel
{
    public long RequestArticleId { get; set; }
    public string Status { get; set; } = null!; // E.g., "Approved", "Rejected", "Pending"
    public string? AdminNotes { get; set; }
}

public sealed class RequestArticleListReqModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? SearchKeyword { get; set; } // By PitcherName or Email
}
