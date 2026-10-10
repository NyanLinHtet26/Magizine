using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblArticleCategory
{
    public string ArticleCategoryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual ICollection<TblAd> TblAds { get; set; } = new List<TblAd>();

    public virtual ICollection<TblArticle> TblArticles { get; set; } = new List<TblArticle>();

    public virtual ICollection<TblRequestArticle> TblRequestArticles { get; set; } = new List<TblRequestArticle>();
}
