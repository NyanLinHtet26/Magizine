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

    public virtual DbSet<TblAuthor> TblAuthors { get; set; }

    public virtual DbSet<TblContactMessage> TblContactMessages { get; set; }

    public virtual DbSet<TblNewsletter> TblNewsletters { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblRequestArticle> TblRequestArticles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone", "recovery_code" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in", "like", "ilike", "is", "match", "imatch", "isdistinct" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<TblAboutAndAppDatum>(entity =>
        {
            entity.HasKey(e => e.AppDataId).HasName("Tbl_AboutAndAppData_pkey");

            entity.ToTable("Tbl_AboutAndAppData");

            entity.HasIndex(e => e.Key, "Tbl_AboutAndAppData_Key_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.GroupName).HasMaxLength(50);
            entity.Property(e => e.Key).HasMaxLength(100);

            entity.HasOne(d => d.UpdatedByAdmin).WithMany(p => p.TblAboutAndAppData)
                .HasForeignKey(d => d.UpdatedByAdminId)
                .HasConstraintName("Tbl_AboutAndAppData_UpdatedByAdminId_fkey");
        });

        modelBuilder.Entity<TblAd>(entity =>
        {
            entity.HasKey(e => e.AdId).HasName("Tbl_Ads_pkey");

            entity.ToTable("Tbl_Ads");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ImageUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Placement).HasMaxLength(50);
            entity.Property(e => e.TargetUrl).HasMaxLength(300);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblAds)
                .HasForeignKey(d => d.ArticleCategoryId)
                .HasConstraintName("Tbl_Ads_ArticleCategoryId_fkey");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblAds)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Ads_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblAdmin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("Tbl_Admin_pkey");

            entity.ToTable("Tbl_Admin");

            entity.HasIndex(e => e.Email, "Tbl_Admin_Email_key").IsUnique();

            entity.HasIndex(e => e.Username, "Tbl_Admin_Username_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(30)
                .HasDefaultValueSql("'Editor'::character varying");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.InverseDeletedByAdmin)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Admin_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblArticle>(entity =>
        {
            entity.HasKey(e => e.ArticleId).HasName("Tbl_Article_pkey");

            entity.ToTable("Tbl_Article");

            entity.HasIndex(e => e.Slug, "Tbl_Article_Slug_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PhotoCaption).HasMaxLength(200);
            entity.Property(e => e.PhotoCredit).HasMaxLength(100);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.Slug).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Draft'::character varying");
            entity.Property(e => e.SubTitle).HasMaxLength(400);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.ArticleCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Tbl_Article_ArticleCategoryId_fkey");

            entity.HasOne(d => d.Author).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Tbl_Article_AuthorId_fkey");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblArticles)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Article_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblArticleCategory>(entity =>
        {
            entity.HasKey(e => e.ArticleCategoryId).HasName("Tbl_ArticleCategory_pkey");

            entity.ToTable("Tbl_ArticleCategory");

            entity.HasIndex(e => e.Name, "Tbl_ArticleCategory_Name_key").IsUnique();

            entity.HasIndex(e => e.Slug, "Tbl_ArticleCategory_Slug_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(20);
            entity.Property(e => e.Slug).HasMaxLength(30);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblArticleCategories)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_ArticleCategory_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblAuthor>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("Tbl_Author_pkey");

            entity.ToTable("Tbl_Author");

            entity.HasIndex(e => e.Email, "Tbl_Author_Email_key").IsUnique();

            entity.HasIndex(e => e.Slug, "Tbl_Author_Slug_key").IsUnique();

            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.InstagramUrl).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhotoUrl).HasMaxLength(300);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.TwitterUrl).HasMaxLength(300);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(300);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblAuthors)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Author_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblContactMessage>(entity =>
        {
            entity.HasKey(e => e.ContactMessageId).HasName("Tbl_ContactMessage_pkey");

            entity.ToTable("Tbl_ContactMessage");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.SenderEmail).HasMaxLength(100);
            entity.Property(e => e.SenderName).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(200);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblContactMessageDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_ContactMessage_DeletedByAdminId_fkey");

            entity.HasOne(d => d.ReadByAdmin).WithMany(p => p.TblContactMessageReadByAdmins)
                .HasForeignKey(d => d.ReadByAdminId)
                .HasConstraintName("Tbl_ContactMessage_ReadByAdminId_fkey");
        });

        modelBuilder.Entity<TblNewsletter>(entity =>
        {
            entity.HasKey(e => e.NewsletterId).HasName("Tbl_Newsletter_pkey");

            entity.ToTable("Tbl_Newsletter");

            entity.HasIndex(e => e.Email, "Tbl_Newsletter_Email_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubscribedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblNewsletters)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Newsletter_DeletedByAdminId_fkey");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Tbl_Notification_pkey");

            entity.ToTable("Tbl_Notification");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Type).HasMaxLength(30);

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblNotificationDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_Notification_DeletedByAdminId_fkey");

            entity.HasOne(d => d.RecipientAdmin).WithMany(p => p.TblNotificationRecipientAdmins)
                .HasForeignKey(d => d.RecipientAdminId)
                .HasConstraintName("Tbl_Notification_RecipientAdminId_fkey");

            entity.HasOne(d => d.RecipientAuthor).WithMany(p => p.TblNotifications)
                .HasForeignKey(d => d.RecipientAuthorId)
                .HasConstraintName("Tbl_Notification_RecipientAuthorId_fkey");
        });

        modelBuilder.Entity<TblRequestArticle>(entity =>
        {
            entity.HasKey(e => e.RequestArticleId).HasName("Tbl_RequestArticle_pkey");

            entity.ToTable("Tbl_RequestArticle");

            entity.Property(e => e.AdminNotes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.PitcherEmail).HasMaxLength(100);
            entity.Property(e => e.PitcherName).HasMaxLength(100);
            entity.Property(e => e.ProposedTitle).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Pending'::character varying");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ArticleCategory).WithMany(p => p.TblRequestArticles)
                .HasForeignKey(d => d.ArticleCategoryId)
                .HasConstraintName("Tbl_RequestArticle_ArticleCategoryId_fkey");

            entity.HasOne(d => d.DeletedByAdmin).WithMany(p => p.TblRequestArticleDeletedByAdmins)
                .HasForeignKey(d => d.DeletedByAdminId)
                .HasConstraintName("Tbl_RequestArticle_DeletedByAdminId_fkey");

            entity.HasOne(d => d.ReviewedByAdmin).WithMany(p => p.TblRequestArticleReviewedByAdmins)
                .HasForeignKey(d => d.ReviewedByAdminId)
                .HasConstraintName("Tbl_RequestArticle_ReviewedByAdminId_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
