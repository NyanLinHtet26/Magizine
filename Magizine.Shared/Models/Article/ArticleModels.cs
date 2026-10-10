using Magizine.Shared.Models.Paging;

namespace Magizine.Shared.Models.Article;

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
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }
    
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
    public string? PhotoUrl { get; set; }
    public string? PhotoCaption { get; set; }
    public string? PhotoCredit { get; set; }
    
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
