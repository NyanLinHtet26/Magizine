using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblArticleCategory
{
    public long ArticleCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual ICollection<TblAd> TblAds { get; set; } = new List<TblAd>();

    public virtual ICollection<TblArticle> TblArticles { get; set; } = new List<TblArticle>();

    public virtual ICollection<TblRequestArticle> TblRequestArticles { get; set; } = new List<TblRequestArticle>();
}
