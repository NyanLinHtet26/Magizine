using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAuthor
{
    public string AuthorId { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Title { get; set; }

    public string? Bio { get; set; }

    public string? PhotoUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string? TwitterUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public string Slug { get; set; } = null!;

    public bool IsApproved { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual ICollection<TblArticle> TblArticles { get; set; } = new List<TblArticle>();

    public virtual ICollection<TblNotification> TblNotifications { get; set; } = new List<TblNotification>();

    public virtual ICollection<TblRequestArticle> TblRequestArticles { get; set; } = new List<TblRequestArticle>();
}
