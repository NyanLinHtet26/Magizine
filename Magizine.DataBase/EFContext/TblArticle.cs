using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblArticle
{
    public long ArticleId { get; set; }

    public string Title { get; set; } = null!;

    public string? SubTitle { get; set; }

    public string Slug { get; set; } = null!;

    public string? Body { get; set; }

    public string? PhotoUrl { get; set; }

    public string? PhotoCaption { get; set; }

    public string? PhotoCredit { get; set; }

    public long ArticleCategoryId { get; set; }

    public long AuthorId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsSpotlight { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory ArticleCategory { get; set; } = null!;

    public virtual TblAuthor Author { get; set; } = null!;

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
