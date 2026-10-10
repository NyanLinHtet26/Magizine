using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblArticlePhoto
{
    public string ArticlePhotoId { get; set; } = null!;

    public string? ArticleId { get; set; }

    public string? RequestArticleId { get; set; }

    public string PhotoUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public string? Credit { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblArticle? Article { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual TblRequestArticle? RequestArticle { get; set; }
}
