using Magizine.Shared.Models.Paging;

namespace Magizine.Shared.Models.RequestArticle;

public sealed class RequestArticleResModel
{
    public string RequestArticleId { get; set; } = null!;
    public string PitcherName { get; set; } = null!;
    public string PitcherEmail { get; set; } = null!;
    public string? ProposedTitle { get; set; }
    public string PitchBody { get; set; } = null!;
    public string? ArticleCategoryId { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedByAdminId { get; set; }
    public string? AdminNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class RequestArticleDetailReqModel
{
    public string RequestArticleId { get; set; } = null!;
}

public sealed class CreateRequestArticleReqModel
{
    public string PitcherName { get; set; } = null!;
    public string PitcherEmail { get; set; } = null!;
    public string? ProposedTitle { get; set; }
    public string PitchBody { get; set; } = null!;
    public string? ArticleCategoryId { get; set; }
}

public sealed class ReviewRequestArticleReqModel
{
    public string RequestArticleId { get; set; } = null!;
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
