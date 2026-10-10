using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Magizine.DataBase.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblAboutAndAppDatum> TblAboutAndAppData { get; set; }

    public virtual DbSet<TblAd> TblAds { get; set; }

    public virtual DbSet<TblAdmin> TblAdmins { get; set; }

    public virtual DbSet<TblArticle> TblArticles { get; set; }

    public virtual DbSet<TblArticleCategory> TblArticleCategories { get; set; }

    public virtual DbSet<TblArticlePhoto> TblArticlePhotos { get; set; }

    public virtual DbSet<TblAuthor> TblAuthors { get; set; }

    public virtual DbSet<TblContactMessage> TblContactMessages { get; set; }

    public virtual DbSet<TblNewsletter> TblNewsletters { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblRequestArticle> TblRequestArticles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblAboutAndAppDatum>(entity =>
        {
            entity.HasKey(e => e.AppDataId).HasName("PK__Tbl_Abou__4E459D2F3B499A0A");

            entity.ToTable("Tbl_AboutAndAppData");

            entity.HasIndex(e => e.Key, "UQ_AboutAndAppData_Key").IsUnique();

            entity.Property(e => e.AppDataId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_AboutAndAppData_CreatedAt");
            entity.Property(e => e.GroupName).HasMaxLength(50);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.UpdatedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);

            entity.HasOne(d => d.UpdatedByAdmin).WithMany(p => p.TblAboutAndAppData)
                .HasForeignKey(d => d.UpdatedByAdminId)
                .HasConstraintName("FK_AboutAndAppData_UpdatedByAdmin");
        });

        modelBuilder.Entity<TblAd>(entity =>
        {
            entity.HasKey(e => e.AdId).HasName("PK__Tbl_Ads__7130D5AEDA122729");

            entity.ToTable("Tbl_Ads");

            entity.HasIndex(e => e.ArticleCategoryId, "IX_Tbl_Ads_Category").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.Placement, e.IsActive }, "IX_Tbl_Ads_Placement").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.AdId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ArticleCategoryId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Ads_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ImageUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Ads_IsActive");
            entity.Property(e => e.Placement).HasMaxLength(50);
            entity.Property(e => e.TargetUrl).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblAds)
                .HasForeignKey(d => d.ArticleCategoryId)
                .HasConstraintName("FK_Ads_Category");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblAds)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Ads_DeletedByAdmin");
        });

        modelBuilder.Entity<TblAdmin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Tbl_Admi__719FE488946908E5");

            entity.ToTable("Tbl_Admin");

            entity.HasIndex(e => e.Email, "UX_Tbl_Admin_Email_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Username, "UX_Tbl_Admin_Username_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.AdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Admin_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Admin_IsActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValue("Editor", "DF_Admin_Role");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.InverseDeletedByAdmin)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Admin_DeletedByAdmin");
        });

        modelBuilder.Entity<TblArticle>(entity =>
        {
            entity.HasKey(e => e.ArticleId).HasName("PK__Tbl_Arti__9C6270E8ABD2F021");

            entity.ToTable("Tbl_Article");

            entity.HasIndex(e => e.AuthorId, "IX_Tbl_Article_AuthorId");

            entity.HasIndex(e => e.ArticleCategoryId, "IX_Tbl_Article_CategoryId");

            entity.HasIndex(e => new { e.Status, e.PublishedAt }, "IX_Tbl_Article_Status")
                .IsDescending(false, true)
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Slug, "UX_Tbl_Article_Slug_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.ArticleId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ArticleCategoryId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.AuthorId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Article_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.PhotoCaption).HasMaxLength(200);
            entity.Property(e => e.PhotoCredit).HasMaxLength(100);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft", "DF_Article_Status");
            entity.Property(e => e.SubTitle).HasMaxLength(400);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.ArticleCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Article_Category");

            entity.HasOne(d => d.Author).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Article_Author");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Article_DeletedByAdmin");
        });

        modelBuilder.Entity<TblArticleCategory>(entity =>
        {
            entity.HasKey(e => e.ArticleCategoryId).HasName("PK__Tbl_Arti__E0B6094314D46408");

            entity.ToTable("Tbl_ArticleCategory");

            entity.HasIndex(e => e.Name, "UX_Tbl_ArticleCategory_Name_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Slug, "UX_Tbl_ArticleCategory_Slug_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.ArticleCategoryId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ArticleCat_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Slug).HasMaxLength(30);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblArticleCategories)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_ArticleCat_DeletedByAdmin");
        });

        modelBuilder.Entity<TblArticlePhoto>(entity =>
        {
            entity.HasKey(e => e.ArticlePhotoId).HasName("PK__Tbl_Arti__8083531A0FB1A6A2");

            entity.ToTable("Tbl_ArticlePhoto");

            entity.HasIndex(e => new { e.ArticleId, e.SortOrder }, "IX_Tbl_ArticlePhoto_ArticleId").HasFilter("([IsDeleted]=(0) AND [ArticleId] IS NOT NULL)");

            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt }, "IX_Tbl_ArticlePhoto_Cleanup").HasFilter("([IsDeleted]=(1))");

            entity.HasIndex(e => new { e.RequestArticleId, e.SortOrder }, "IX_Tbl_ArticlePhoto_RequestArticleId").HasFilter("([IsDeleted]=(0) AND [RequestArticleId] IS NOT NULL)");

            entity.Property(e => e.ArticlePhotoId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ArticleId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Caption).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ArticlePhoto_CreatedAt");
            entity.Property(e => e.Credit).HasMaxLength(100);
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.RequestArticleId)
                .HasMaxLength(26)
                .IsUnicode(false);

            entity.HasOne(d => d.Article).WithMany(p => p.TblArticlePhotos)
                .HasForeignKey(d => d.ArticleId)
                .HasConstraintName("FK_ArticlePhoto_Article");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblArticlePhotos)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_ArticlePhoto_DeletedByAdmin");

            entity.HasOne(d => d.RequestArticle).WithMany(p => p.TblArticlePhotos)
                .HasForeignKey(d => d.RequestArticleId)
                .HasConstraintName("FK_ArticlePhoto_RequestArticle");
        });

        modelBuilder.Entity<TblAuthor>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("PK__Tbl_Auth__70DAFC34D71EC286");

            entity.ToTable("Tbl_Author");

            entity.HasIndex(e => e.Email, "UX_Tbl_Author_Email_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Slug, "UX_Tbl_Author_Slug_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.AuthorId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Author_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.InstagramUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Author_IsActive");
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.TwitterUrl).HasMaxLength(300);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(300);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblAuthors)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Author_DeletedByAdmin");
        });

        modelBuilder.Entity<TblContactMessage>(entity =>
        {
            entity.HasKey(e => e.ContactMessageId).HasName("PK__Tbl_Cont__2B0D4DFCE16A209A");

            entity.ToTable("Tbl_ContactMessage");

            entity.HasIndex(e => e.IsRead, "IX_Tbl_ContactMessage_IsRead").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.ContactMessageId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_ContactMessage_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ReadByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.SenderEmail).HasMaxLength(100);
            entity.Property(e => e.SenderName).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblContactMessageDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_ContactMessage_DeletedByAdmin");

            entity.HasOne(d => d.ReadByAdmin).WithMany(p => p.TblContactMessageReadByAdmins)
                .HasForeignKey(d => d.ReadByAdminId)
                .HasConstraintName("FK_ContactMessage_ReadByAdmin");
        });

        modelBuilder.Entity<TblNewsletter>(entity =>
        {
            entity.HasKey(e => e.NewsletterId).HasName("PK__Tbl_News__34A1DFFDCD7CE8F4");

            entity.ToTable("Tbl_Newsletter");

            entity.HasIndex(e => e.Email, "UX_Tbl_Newsletter_Email_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.NewsletterId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Newsletter_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Newsletter_IsActive");
            entity.Property(e => e.SubscribedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Newsletter_SubscribedAt");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblNewsletters)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Newsletter_DeletedByAdmin");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Tbl_Noti__20CF2E125CA7FA45");

            entity.ToTable("Tbl_Notification");

            entity.HasIndex(e => new { e.RecipientAdminId, e.IsRead }, "IX_Tbl_Notification_Admin").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.RecipientAuthorId, e.IsRead }, "IX_Tbl_Notification_Author").HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.NotificationId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_Notification_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.RecipientAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.RecipientAuthorId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.RelatedEntityId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Type).HasMaxLength(30);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblNotificationDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_Notification_DeletedByAdmin");

            entity.HasOne(d => d.RecipientAdmin).WithMany(p => p.TblNotificationRecipientAdmins)
                .HasForeignKey(d => d.RecipientAdminId)
                .HasConstraintName("FK_Notification_Admin");

            entity.HasOne(d => d.RecipientAuthor).WithMany(p => p.TblNotifications)
                .HasForeignKey(d => d.RecipientAuthorId)
                .HasConstraintName("FK_Notification_Author");
        });

        modelBuilder.Entity<TblRequestArticle>(entity =>
        {
            entity.HasKey(e => e.RequestArticleId).HasName("PK__Tbl_Requ__A90A9A15CC88F413");

            entity.ToTable("Tbl_RequestArticle");

            entity.HasIndex(e => e.AuthorId, "IX_Tbl_RequestArticle_AuthorId");

            entity.HasIndex(e => e.ArticleCategoryId, "IX_Tbl_RequestArticle_CategoryId");

            entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt }, "IX_Tbl_RequestArticle_Cleanup").HasFilter("([IsDeleted]=(1))");

            entity.HasIndex(e => e.Status, "IX_Tbl_RequestArticle_Status").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.Slug, "UX_Tbl_RequestArticle_Slug_NotDeleted")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.Property(e => e.RequestArticleId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.AdminNotes).HasMaxLength(500);
            entity.Property(e => e.ArticleCategoryId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.AuthorId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetimeoffset())", "DF_RequestArticle_CreatedAt");
            entity.Property(e => e.DeletedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.PhotoCaption).HasMaxLength(200);
            entity.Property(e => e.PhotoCredit).HasMaxLength(100);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.PublishedArticleId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.ReviewedByAdminId)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft", "DF_RequestArticle_Status");
            entity.Property(e => e.SubTitle).HasMaxLength(400);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblRequestArticles)
                .HasForeignKey(d => d.ArticleCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestArticle_Category");

            entity.HasOne(d => d.Author).WithMany(p => p.TblRequestArticles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RequestArticle_Author");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblRequestArticleDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("FK_RequestArticle_DeletedByAdmin");

            entity.HasOne(d => d.PublishedArticle).WithMany(p => p.TblRequestArticles)
                .HasForeignKey(d => d.PublishedArticleId)
                .HasConstraintName("FK_RequestArticle_Article");

            entity.HasOne(d => d.ReviewedByAdmin).WithMany(p => p.TblRequestArticleReviewedByAdmins)
                .HasForeignKey(d => d.ReviewedByAdminId)
                .HasConstraintName("FK_RequestArticle_ReviewedByAdmin");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
