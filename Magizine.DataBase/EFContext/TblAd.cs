using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAd
{
    public long AdId { get; set; }

    public string Title { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string? TargetUrl { get; set; }

    public string Placement { get; set; } = null!;

    public long? ArticleCategoryId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public int ClickCount { get; set; }

    public int ImpressionCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory? ArticleCategory { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
