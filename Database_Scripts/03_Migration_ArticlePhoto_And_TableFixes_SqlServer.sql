-- ============================================================================
-- L'ÉDITION MAGAZINE PLATFORM - INCREMENTAL MIGRATION & TABLE FIX SCRIPT
-- ============================================================================
-- Target Engine : Microsoft SQL Server 2017+ / Azure SQL Database
-- Database      : MagizineDB
-- Safe Mode     : Non-destructive / Idempotent (Preserves existing table data)
--
-- What this script does:
--   1. Enables SET QUOTED_IDENTIFIER ON & SET ANSI_NULLS ON (required for filtered indexes)
--   2. Fixes any missing Filtered Unique & Performance Indexes on existing tables
--   3. Ensures PhotoUrl / ImageUrl columns across Tbl_Author, Tbl_Article, Tbl_Ads are NVARCHAR(300)
--   4. Creates the new dbo.Tbl_ArticlePhoto table (Multi-Photo Gallery per Article)
--   5. Creates/Updates the Dapper Stored Procedure dbo.fn_get_article_photos_by_article_id
--   6. Inserts default seed data idempotently (Author, Articles, RequestArticle, ArticlePhotos)
-- ============================================================================

USE MagizineDB;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. EXISTING TABLE COLUMN FIXES (Ensure PhotoUrl / ImageUrl are NVARCHAR(300))
-- ============================================================================

IF COL_LENGTH('dbo.Tbl_Author', 'PhotoUrl') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ALTER COLUMN PhotoUrl NVARCHAR(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Tbl_Article', 'PhotoUrl') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tbl_Article ALTER COLUMN PhotoUrl NVARCHAR(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Tbl_Ads', 'ImageUrl') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tbl_Ads ALTER COLUMN ImageUrl NVARCHAR(300) NOT NULL;
END;
GO

-- ============================================================================
-- 2. EXISTING TABLE FILTERED INDEX FIXES (Idempotent)
-- ============================================================================

-- Tbl_Admin
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Admin_Username_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Admin'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Username_NotDeleted ON dbo.Tbl_Admin(Username) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Admin_Email_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Admin'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Email_NotDeleted ON dbo.Tbl_Admin(Email) WHERE IsDeleted = 0;
GO

-- Tbl_Author
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Author_Email_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Author'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Email_NotDeleted ON dbo.Tbl_Author(Email) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Author_Slug_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Author'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Slug_NotDeleted ON dbo.Tbl_Author(Slug) WHERE IsDeleted = 0;
GO

-- Tbl_ArticleCategory
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_ArticleCategory_Name_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_ArticleCategory'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Name_NotDeleted ON dbo.Tbl_ArticleCategory(Name) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_ArticleCategory_Slug_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_ArticleCategory'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Slug_NotDeleted ON dbo.Tbl_ArticleCategory(Slug) WHERE IsDeleted = 0;
GO

-- Tbl_Article
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Article_Slug_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Article'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Article_Slug_NotDeleted ON dbo.Tbl_Article(Slug) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Article_CategoryId' AND object_id = OBJECT_ID('dbo.Tbl_Article'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Article_CategoryId ON dbo.Tbl_Article(ArticleCategoryId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Article_AuthorId' AND object_id = OBJECT_ID('dbo.Tbl_Article'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Article_AuthorId ON dbo.Tbl_Article(AuthorId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Article_Status' AND object_id = OBJECT_ID('dbo.Tbl_Article'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Article_Status ON dbo.Tbl_Article(Status, PublishedAt DESC) WHERE IsDeleted = 0;
GO

-- Tbl_RequestArticle
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_RequestArticle_Status' AND object_id = OBJECT_ID('dbo.Tbl_RequestArticle'))
    CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_Status ON dbo.Tbl_RequestArticle(Status) WHERE IsDeleted = 0;
GO

-- Tbl_ContactMessage
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ContactMessage_IsRead' AND object_id = OBJECT_ID('dbo.Tbl_ContactMessage'))
    CREATE NONCLUSTERED INDEX IX_Tbl_ContactMessage_IsRead ON dbo.Tbl_ContactMessage(IsRead) WHERE IsDeleted = 0;
GO

-- Tbl_Newsletter
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tbl_Newsletter_Email_NotDeleted' AND object_id = OBJECT_ID('dbo.Tbl_Newsletter'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Newsletter_Email_NotDeleted ON dbo.Tbl_Newsletter(Email) WHERE IsDeleted = 0;
GO

-- Tbl_Notification
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Notification_Author' AND object_id = OBJECT_ID('dbo.Tbl_Notification'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Author ON dbo.Tbl_Notification(RecipientAuthorId, IsRead) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Notification_Admin' AND object_id = OBJECT_ID('dbo.Tbl_Notification'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Admin ON dbo.Tbl_Notification(RecipientAdminId, IsRead) WHERE IsDeleted = 0;
GO

-- Tbl_Ads
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Ads_Placement' AND object_id = OBJECT_ID('dbo.Tbl_Ads'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Ads_Placement ON dbo.Tbl_Ads(Placement, IsActive) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_Ads_Category' AND object_id = OBJECT_ID('dbo.Tbl_Ads'))
    CREATE NONCLUSTERED INDEX IX_Tbl_Ads_Category ON dbo.Tbl_Ads(ArticleCategoryId) WHERE IsDeleted = 0;
GO


-- ============================================================================
-- 3. NEW TABLE: dbo.Tbl_ArticlePhoto (Multi-Photo Gallery per Article)
-- ============================================================================

IF OBJECT_ID('dbo.Tbl_ArticlePhoto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_ArticlePhoto (
        ArticlePhotoId    VARCHAR(26) NOT NULL PRIMARY KEY,
        ArticleId         VARCHAR(26) NOT NULL CONSTRAINT FK_ArticlePhoto_Article REFERENCES dbo.Tbl_Article(ArticleId),
        PhotoUrl          NVARCHAR(300) NOT NULL,
        Caption           NVARCHAR(200) NULL,
        Credit            NVARCHAR(100) NULL,
        SortOrder         INT NOT NULL CONSTRAINT DF_ArticlePhoto_SortOrder DEFAULT 0,
        CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_ArticlePhoto_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt         DATETIMEOFFSET NULL,
        IsDeleted         BIT NOT NULL CONSTRAINT DF_ArticlePhoto_IsDeleted DEFAULT 0,
        DeletedAt         DATETIMEOFFSET NULL,
        DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_ArticlePhoto_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ArticlePhoto_ArticleId' AND object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_ArticleId
    ON dbo.Tbl_ArticlePhoto(ArticleId, SortOrder ASC)
    WHERE IsDeleted = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ArticlePhoto_Cleanup' AND object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_Cleanup
    ON dbo.Tbl_ArticlePhoto(IsDeleted, DeletedAt)
    WHERE IsDeleted = 1;
END;
GO


-- ============================================================================
-- 4. NEW STORED PROCEDURE: dbo.fn_get_article_photos_by_article_id
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.fn_get_article_photos_by_article_id
    @p_article_id VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.ArticlePhotoId,
        ap.ArticleId,
        ap.PhotoUrl,
        ap.Caption,
        ap.Credit,
        ap.SortOrder,
        ap.CreatedAt
    FROM dbo.Tbl_ArticlePhoto ap
    WHERE ap.ArticleId = @p_article_id
      AND ap.IsDeleted = 0
    ORDER BY ap.SortOrder ASC, ap.CreatedAt ASC;
END;
GO


-- ============================================================================
-- 5. SEED DATA FOR NEW TABLE & TEST RECORDS (Idempotent)
-- ============================================================================

-- Default Author (Matches AuthorizedAuthorId in MagizineAuthor.Api)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Author WHERE AuthorId = '01JAUTHOR00000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_Author (
        AuthorId, FirstName, LastName, Email, PasswordHash, Title, Bio,
        PhotoUrl, InstagramUrl, TwitterUrl, WebsiteUrl, Slug, IsApproved, IsActive, CreatedAt
    )
    VALUES (
        '01JAUTHOR00000000000000001',
        N'Elena',
        N'Rostova',
        N'elena.rostova@ledition.com',
        N'$2a$12$e0NRiA4b2VwYQcT7X8mOVuYqHhFhR8rUj2V0Wb8Z0XlK1mG5o6Pfa',
        N'Senior Editor & Cultural Essayist',
        N'Elena Rostova writes on quiet luxury, minimalist aesthetics, and modern literature.',
        N'https://images.unsplash.com/photo-1534528741775-53994a69daeb',
        N'https://instagram.com/elenarostova',
        N'https://twitter.com/elenarostova',
        N'https://elenarostova.com',
        N'elena-rostova',
        1,
        1,
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Default Articles (Published + Draft)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Article WHERE ArticleId = '01JART00000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_Article (
        ArticleId, Title, SubTitle, Slug, Body, PhotoUrl, PhotoCaption, PhotoCredit,
        ArticleCategoryId, AuthorId, [Status], PublishedAt, IsFeatured, IsSpotlight, CreatedAt
    )
    VALUES
    (
        '01JART00000000000000000001',
        N'The Art of Quiet Luxury',
        N'Why understatement remains the highest form of modern elegance',
        N'the-art-of-quiet-luxury',
        N'<p>True refinement rarely announces itself. Across architecture, skincare, and wardrobe curation, a return to tactile craftsmanship is redefining modern luxury.</p>',
        N'https://images.unsplash.com/photo-1600585154340-be6161a56a0c',
        N'Morning light across raw linen and travertine.',
        N'Photo by L''ÉDITION Studio',
        '01JCAT00000000000000000001',
        '01JAUTHOR00000000000000001',
        N'Published',
        SYSDATETIMEOFFSET(),
        1,
        1,
        SYSDATETIMEOFFSET()
    ),
    (
        '01JART00000000000000000002',
        N'Slow Morning Rituals',
        N'Cultivating stillness before the digital day begins',
        N'slow-morning-rituals',
        N'<p>Before the first notification arrives, the first hour of the morning offers a sanctuary for reflection and intentional habits.</p>',
        N'https://images.unsplash.com/photo-1495474472287-4d71bcdd2085',
        N'Handcrafted ceramic pour-over vessel at dawn.',
        N'Photo by L''ÉDITION Studio',
        '01JCAT00000000000000000002',
        '01JAUTHOR00000000000000001',
        N'Draft',
        NULL,
        0,
        0,
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Default Request Article Pitch
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_RequestArticle WHERE RequestArticleId = '01JREQ00000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_RequestArticle (
        RequestArticleId, PitcherName, PitcherEmail, ProposedTitle, PitchBody,
        ArticleCategoryId, [Status], SubmittedAt, CreatedAt
    )
    VALUES (
        '01JREQ00000000000000000001',
        N'Julian Mercer',
        N'julian.mercer@example.com',
        N'Monastic Architecture in Contemporary Homes',
        N'An essay examining how contemporary architects draw inspiration from Cistercian monasteries to create serene urban residences.',
        '01JCAT00000000000000000003',
        N'Pending',
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Default Article Gallery Photos (Tbl_ArticlePhoto)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ArticlePhoto WHERE ArticlePhotoId = '01JAPHT0000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_ArticlePhoto (
        ArticlePhotoId, ArticleId, PhotoUrl, Caption, Credit, SortOrder, CreatedAt
    )
    VALUES
    (
        '01JAPHT0000000000000000001',
        '01JART00000000000000000001',
        N'https://images.unsplash.com/photo-1578749556568-bc2c40e68b61',
        N'Handcrafted stoneware vessels in afternoon shadow.',
        N'Photo by L''ÉDITION Studio',
        1,
        SYSDATETIMEOFFSET()
    ),
    (
        '01JAPHT0000000000000000002',
        '01JART00000000000000000001',
        N'https://images.unsplash.com/photo-1513694203232-719a280e022f',
        N'Minimalist limestone courtyard in Kyoto.',
        N'Photo by Kenji Sato',
        2,
        SYSDATETIMEOFFSET()
    );
END;
GO
