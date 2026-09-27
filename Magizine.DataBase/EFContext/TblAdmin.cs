using System;
using System.Collections.Generic;

namespace Magizine.DataBase.Models;

public partial class TblAdmin
{
    public long AdminId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedByAdminId { get; set; }

    public virtual TblAdmin? DeletedByAdmin { get; set; }

    public virtual ICollection<TblAdmin> InverseDeletedByAdmin { get; set; } = new List<TblAdmin>();

    public virtual ICollection<TblAboutAndAppDatum> TblAboutAndAppData { get; set; } = new List<TblAboutAndAppDatum>();

    public virtual ICollection<TblAd> TblAds { get; set; } = new List<TblAd>();

    public virtual ICollection<TblArticleCategory> TblArticleCategories { get; set; } = new List<TblArticleCategory>();

    public virtual ICollection<TblArticle> TblArticles { get; set; } = new List<TblArticle>();

    public virtual ICollection<TblAuthor> TblAuthors { get; set; } = new List<TblAuthor>();

    public virtual ICollection<TblContactMessage> TblContactMessageDeletedByAdmins { get; set; } = new List<TblContactMessage>();

    public virtual ICollection<TblContactMessage> TblContactMessageReadByAdmins { get; set; } = new List<TblContactMessage>();

    public virtual ICollection<TblNewsletter> TblNewsletters { get; set; } = new List<TblNewsletter>();

    public virtual ICollection<TblNotification> TblNotificationDeletedByAdmins { get; set; } = new List<TblNotification>();

    public virtual ICollection<TblNotification> TblNotificationRecipientAdmins { get; set; } = new List<TblNotification>();

    public virtual ICollection<TblRequestArticle> TblRequestArticleDeletedByAdmins { get; set; } = new List<TblRequestArticle>();

    public virtual ICollection<TblRequestArticle> TblRequestArticleReviewedByAdmins { get; set; } = new List<TblRequestArticle>();
}
