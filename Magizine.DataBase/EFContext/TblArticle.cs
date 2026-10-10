using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblArticle
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

    public string AuthorId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTimeOffset? PublishedAt { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsSpotlight { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory ArticleCategory { get; set; } = null!;

    public virtual TblAuthor Author { get; set; } = null!;

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual ICollection<TblArticlePhoto> TblArticlePhotos { get; set; } = new List<TblArticlePhoto>();

    public virtual ICollection<TblRequestArticle> TblRequestArticles { get; set; } = new List<TblRequestArticle>();
}
