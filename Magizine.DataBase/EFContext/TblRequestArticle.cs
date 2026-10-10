using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblRequestArticle
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

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblArticleCategory? ArticleCategory { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblAdmin? ReviewedByAdmin { get; set; }
}
