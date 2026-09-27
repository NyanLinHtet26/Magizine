using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblRequestArticle
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

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory? ArticleCategory { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? ReviewedByAdmin { get; set; }
}
