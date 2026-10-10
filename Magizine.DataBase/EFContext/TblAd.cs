using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAd
{
    public string AdId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string? TargetUrl { get; set; }

    public string Placement { get; set; } = null!;

    public string? ArticleCategoryId { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public bool IsActive { get; set; }

    public int ClickCount { get; set; }

    public int ImpressionCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory? ArticleCategory { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }
}
