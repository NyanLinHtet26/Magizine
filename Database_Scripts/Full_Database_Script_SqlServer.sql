-- ============================================================================
-- L'ÉDITION MAGAZINE PLATFORM - SQL SERVER (T-SQL) FULL DATABASE SCRIPT (ULID)
-- ============================================================================
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- Identifier Type: ULID (Universally Unique Lexicographically Sortable Identifier)
-- Storage Type: VARCHAR(26) (Crockford Base32 26-char string)
-- 
-- Contents:
--   1. Clean Drop Hierarchy (Reverse Dependency Order to prevent FK errors)
--   2. 10 Tables with ULID Primary Keys, Types, Defaults, and Foreign Keys
--   3. Filtered Unique Indexes (Partial Indexes) for Soft-Delete Tables
--   4. Foreign Key & Query Performance Indexes
--   5. Stored Procedures (T-SQL for Dapper Integration - 10 Procedures)
--   6. Initial Seed Data (SuperAdmin, Default Categories, Site Settings)
-- ============================================================================

-- ============================================================================
-- 1. DROP EXISTING TABLES (Reverse Dependency Order)
-- ============================================================================


use MagizineDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID('dbo.Tbl_ReaderBookmark', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ReaderBookmark;
IF OBJECT_ID('dbo.Tbl_Reader', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Reader;
IF OBJECT_ID('dbo.Tbl_Ads', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Ads;
IF OBJECT_ID('dbo.Tbl_Notification', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Notification;
IF OBJECT_ID('dbo.Tbl_AboutAndAppData', 'U') IS NOT NULL DROP TABLE dbo.Tbl_AboutAndAppData;
IF OBJECT_ID('dbo.Tbl_Newsletter', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Newsletter;
IF OBJECT_ID('dbo.Tbl_ContactMessage', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ContactMessage;
IF OBJECT_ID('dbo.Tbl_ArticlePhoto', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ArticlePhoto;
IF OBJECT_ID('dbo.Tbl_RequestArticle', 'U') IS NOT NULL DROP TABLE dbo.Tbl_RequestArticle;
IF OBJECT_ID('dbo.Tbl_Article', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Article;
IF OBJECT_ID('dbo.Tbl_ArticleCategory', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ArticleCategory;
IF OBJECT_ID('dbo.Tbl_Author', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Author;
IF OBJECT_ID('dbo.Tbl_Admin', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Admin;
GO

-- ============================================================================
-- 2. DDL - TABLE DEFINITIONS & INDEXES (Dependency Order)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Tbl_Admin
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Admin (
    AdminId                       VARCHAR(26) NOT NULL PRIMARY KEY,
    Username                      NVARCHAR(50) NOT NULL,
    Email                         NVARCHAR(100) NOT NULL,
    PasswordHash                  NVARCHAR(255) NOT NULL,
    Role                          NVARCHAR(30) NOT NULL CONSTRAINT DF_Admin_Role DEFAULT N'Editor',
    RefreshTokenHash              NVARCHAR(128) NULL,
    RefreshTokenExpiresAt         DATETIMEOFFSET NULL,
    PreviousRefreshTokenHash      NVARCHAR(128) NULL,
    PreviousRefreshTokenExpiresAt DATETIMEOFFSET NULL,
    LastLoginAt                   DATETIMEOFFSET NULL,
    IsActive                      BIT NOT NULL CONSTRAINT DF_Admin_IsActive DEFAULT 1,
    CreatedAt                     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Admin_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt                     DATETIMEOFFSET NULL,
    IsDeleted                     BIT NOT NULL CONSTRAINT DF_Admin_IsDeleted DEFAULT 0,
    DeletedAt                     DATETIMEOFFSET NULL,
    DeletedByAdminId              VARCHAR(26) NULL CONSTRAINT FK_Admin_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Username_NotDeleted     ON dbo.Tbl_Admin(Username)                 WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Email_NotDeleted        ON dbo.Tbl_Admin(Email)                    WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Admin_RefreshTokenHash         ON dbo.Tbl_Admin(RefreshTokenHash)         WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Admin_PreviousRefreshTokenHash ON dbo.Tbl_Admin(PreviousRefreshTokenHash) WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 2. Tbl_Author
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Author (
    AuthorId                      VARCHAR(26) NOT NULL PRIMARY KEY,
    FirstName                     NVARCHAR(50) NOT NULL,
    LastName                      NVARCHAR(50) NOT NULL,
    Email                         NVARCHAR(100) NOT NULL,
    PasswordHash                  NVARCHAR(255) NOT NULL,
    Title                         NVARCHAR(100) NULL,
    Bio                           NVARCHAR(500) NULL,
    PhotoUrl                      NVARCHAR(300) NULL,
    InstagramUrl                  NVARCHAR(300) NULL,
    TwitterUrl                    NVARCHAR(300) NULL,
    WebsiteUrl                    NVARCHAR(300) NULL,
    Slug                          NVARCHAR(100) NOT NULL,
    RefreshTokenHash              NVARCHAR(128) NULL,
    RefreshTokenExpiresAt         DATETIMEOFFSET NULL,
    PreviousRefreshTokenHash      NVARCHAR(128) NULL,
    PreviousRefreshTokenExpiresAt DATETIMEOFFSET NULL,
    LastLoginAt                   DATETIMEOFFSET NULL,
    IsApproved                    BIT NOT NULL CONSTRAINT DF_Author_IsApproved DEFAULT 0,
    IsActive                      BIT NOT NULL CONSTRAINT DF_Author_IsActive DEFAULT 1,
    CreatedAt                     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Author_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt                     DATETIMEOFFSET NULL,
    IsDeleted                     BIT NOT NULL CONSTRAINT DF_Author_IsDeleted DEFAULT 0,
    DeletedAt                     DATETIMEOFFSET NULL,
    DeletedByAdminId              VARCHAR(26) NULL CONSTRAINT FK_Author_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Email_NotDeleted         ON dbo.Tbl_Author(Email)                    WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Slug_NotDeleted          ON dbo.Tbl_Author(Slug)                     WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Author_RefreshTokenHash         ON dbo.Tbl_Author(RefreshTokenHash)         WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Author_PreviousRefreshTokenHash ON dbo.Tbl_Author(PreviousRefreshTokenHash) WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 3. Tbl_ArticleCategory
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_ArticleCategory (
    ArticleCategoryId VARCHAR(26) NOT NULL PRIMARY KEY,
    Name              NVARCHAR(20) NOT NULL,
    Slug              NVARCHAR(30) NOT NULL,
    Description       NVARCHAR(200) NULL,
    SortOrder         INT NOT NULL CONSTRAINT DF_ArticleCat_SortOrder DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_ArticleCat_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_ArticleCat_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_ArticleCat_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Name_NotDeleted ON dbo.Tbl_ArticleCategory(Name) WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Slug_NotDeleted ON dbo.Tbl_ArticleCategory(Slug) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 4. Tbl_Article
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Article (
    ArticleId         VARCHAR(26) NOT NULL PRIMARY KEY,
    Title             NVARCHAR(200) NOT NULL,
    SubTitle          NVARCHAR(400) NULL,
    Slug              NVARCHAR(200) NOT NULL,
    Body              NVARCHAR(MAX) NULL,
    PhotoUrl          NVARCHAR(300) NULL,
    PhotoCaption      NVARCHAR(200) NULL,
    PhotoCredit       NVARCHAR(100) NULL,
    ArticleCategoryId VARCHAR(26) NOT NULL CONSTRAINT FK_Article_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    AuthorId          VARCHAR(26) NOT NULL CONSTRAINT FK_Article_Author REFERENCES dbo.Tbl_Author(AuthorId),
    Status            NVARCHAR(20) NOT NULL CONSTRAINT DF_Article_Status DEFAULT N'Draft',
    PublishedAt       DATETIMEOFFSET NULL,
    IsFeatured        BIT NOT NULL CONSTRAINT DF_Article_IsFeatured DEFAULT 0,
    IsSpotlight       BIT NOT NULL CONSTRAINT DF_Article_IsSpotlight DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Article_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Article_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_Article_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Article_Slug_NotDeleted ON dbo.Tbl_Article(Slug) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_Article_CategoryId             ON dbo.Tbl_Article(ArticleCategoryId);
CREATE NONCLUSTERED INDEX IX_Tbl_Article_AuthorId               ON dbo.Tbl_Article(AuthorId);
CREATE NONCLUSTERED INDEX IX_Tbl_Article_Status                 ON dbo.Tbl_Article(Status, PublishedAt DESC) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 5. Tbl_RequestArticle (Author Staging & Admin Approval Queue)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_RequestArticle (
    RequestArticleId   VARCHAR(26) NOT NULL PRIMARY KEY,
    Title              NVARCHAR(200) NOT NULL,
    SubTitle           NVARCHAR(400) NULL,
    Slug               NVARCHAR(200) NOT NULL,
    Body               NVARCHAR(MAX) NULL,
    PhotoUrl           NVARCHAR(300) NULL,
    PhotoCaption       NVARCHAR(200) NULL,
    PhotoCredit        NVARCHAR(100) NULL,
    ArticleCategoryId  VARCHAR(26) NOT NULL CONSTRAINT FK_RequestArticle_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    AuthorId           VARCHAR(26) NOT NULL CONSTRAINT FK_RequestArticle_Author REFERENCES dbo.Tbl_Author(AuthorId),
    Status             NVARCHAR(20) NOT NULL CONSTRAINT DF_RequestArticle_Status DEFAULT N'Draft',
    SubmittedAt        DATETIMEOFFSET NULL,
    ReviewedAt         DATETIMEOFFSET NULL,
    ReviewedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_RequestArticle_ReviewedByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    AdminNotes         NVARCHAR(500) NULL,
    PublishedArticleId VARCHAR(26) NULL CONSTRAINT FK_RequestArticle_Article REFERENCES dbo.Tbl_Article(ArticleId),
    CreatedAt          DATETIMEOFFSET NOT NULL CONSTRAINT DF_RequestArticle_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt          DATETIMEOFFSET NULL,
    IsDeleted          BIT NOT NULL CONSTRAINT DF_RequestArticle_IsDeleted DEFAULT 0,
    DeletedAt          DATETIMEOFFSET NULL,
    DeletedByAdminId   VARCHAR(26) NULL CONSTRAINT FK_RequestArticle_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_RequestArticle_Slug_NotDeleted ON dbo.Tbl_RequestArticle(Slug) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_CategoryId             ON dbo.Tbl_RequestArticle(ArticleCategoryId);
CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_AuthorId               ON dbo.Tbl_RequestArticle(AuthorId);
CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_Status                 ON dbo.Tbl_RequestArticle(Status) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 5B. Tbl_ArticlePhoto (Multiple Photos / Gallery for RequestArticle & Article)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_ArticlePhoto (
    ArticlePhotoId    VARCHAR(26) NOT NULL PRIMARY KEY,
    ArticleId         VARCHAR(26) NULL CONSTRAINT FK_ArticlePhoto_Article REFERENCES dbo.Tbl_Article(ArticleId),
    RequestArticleId  VARCHAR(26) NULL CONSTRAINT FK_ArticlePhoto_RequestArticle REFERENCES dbo.Tbl_RequestArticle(RequestArticleId),
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
GO

CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_ArticleId        ON dbo.Tbl_ArticlePhoto(ArticleId, SortOrder ASC)        WHERE IsDeleted = 0 AND ArticleId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_RequestArticleId ON dbo.Tbl_ArticlePhoto(RequestArticleId, SortOrder ASC) WHERE IsDeleted = 0 AND RequestArticleId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_Cleanup          ON dbo.Tbl_ArticlePhoto(IsDeleted, DeletedAt)            WHERE IsDeleted = 1;
GO

-- ----------------------------------------------------------------------------
-- 6. Tbl_ContactMessage
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_ContactMessage (
    ContactMessageId  VARCHAR(26) NOT NULL PRIMARY KEY,
    SenderName        NVARCHAR(100) NOT NULL,
    SenderEmail       NVARCHAR(100) NOT NULL,
    Subject           NVARCHAR(200) NULL,
    MessageBody       NVARCHAR(MAX) NOT NULL,
    IsRead            BIT NOT NULL CONSTRAINT DF_ContactMessage_IsRead DEFAULT 0,
    ReadAt            DATETIMEOFFSET NULL,
    ReadByAdminId     VARCHAR(26) NULL CONSTRAINT FK_ContactMessage_ReadByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_ContactMessage_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_ContactMessage_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_ContactMessage_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE NONCLUSTERED INDEX IX_Tbl_ContactMessage_IsRead ON dbo.Tbl_ContactMessage(IsRead) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 7. Tbl_Newsletter
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Newsletter (
    NewsletterId      VARCHAR(26) NOT NULL PRIMARY KEY,
    Email             NVARCHAR(100) NOT NULL,
    FullName          NVARCHAR(100) NULL,
    SubscribedAt      DATETIMEOFFSET NOT NULL CONSTRAINT DF_Newsletter_SubscribedAt DEFAULT SYSDATETIMEOFFSET(),
    IsActive          BIT NOT NULL CONSTRAINT DF_Newsletter_IsActive DEFAULT 1,
    UnsubscribedAt    DATETIMEOFFSET NULL,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Newsletter_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Newsletter_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_Newsletter_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Newsletter_Email_NotDeleted ON dbo.Tbl_Newsletter(Email) WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 8. Tbl_AboutAndAppData (NO Soft-Delete Columns)
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_AboutAndAppData (
    AppDataId         VARCHAR(26) NOT NULL PRIMARY KEY,
    [Key]             NVARCHAR(100) NOT NULL CONSTRAINT UQ_AboutAndAppData_Key UNIQUE,
    [Value]           NVARCHAR(MAX) NULL,
    GroupName         NVARCHAR(50) NULL,
    UpdatedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_AboutAndAppData_UpdatedByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_AboutAndAppData_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL
);
GO

-- ----------------------------------------------------------------------------
-- 9. Tbl_Notification
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Notification (
    NotificationId    VARCHAR(26) NOT NULL PRIMARY KEY,
    RecipientAuthorId VARCHAR(26) NULL CONSTRAINT FK_Notification_Author REFERENCES dbo.Tbl_Author(AuthorId),
    RecipientAdminId  VARCHAR(26) NULL CONSTRAINT FK_Notification_Admin REFERENCES dbo.Tbl_Admin(AdminId),
    Title             NVARCHAR(150) NOT NULL,
    Message           NVARCHAR(500) NOT NULL,
    Type              NVARCHAR(30) NOT NULL,
    IsRead            BIT NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT 0,
    ReadAt            DATETIMEOFFSET NULL,
    RelatedEntityType NVARCHAR(50) NULL,
    RelatedEntityId   VARCHAR(26) NULL,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Notification_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_Notification_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Author ON dbo.Tbl_Notification(RecipientAuthorId, IsRead) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Admin  ON dbo.Tbl_Notification(RecipientAdminId, IsRead)  WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 10. Tbl_Ads
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Ads (
    AdId              VARCHAR(26) NOT NULL PRIMARY KEY,
    Title             NVARCHAR(150) NOT NULL,
    ImageUrl          NVARCHAR(300) NOT NULL,
    TargetUrl         NVARCHAR(300) NULL,
    Placement         NVARCHAR(50) NOT NULL,
    ArticleCategoryId VARCHAR(26) NULL CONSTRAINT FK_Ads_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    StartDate         DATETIMEOFFSET NULL,
    EndDate           DATETIMEOFFSET NULL,
    IsActive          BIT NOT NULL CONSTRAINT DF_Ads_IsActive DEFAULT 1,
    ClickCount        INT NOT NULL CONSTRAINT DF_Ads_ClickCount DEFAULT 0,
    ImpressionCount   INT NOT NULL CONSTRAINT DF_Ads_ImpressionCount DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Ads_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Ads_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  VARCHAR(26) NULL CONSTRAINT FK_Ads_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE NONCLUSTERED INDEX IX_Tbl_Ads_Placement ON dbo.Tbl_Ads(Placement, IsActive) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_Ads_Category  ON dbo.Tbl_Ads(ArticleCategoryId)   WHERE IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 11. Tbl_Reader
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_Reader (
    ReaderId                      VARCHAR(26)       NOT NULL PRIMARY KEY,
    FirstName                     NVARCHAR(50)      NOT NULL,
    LastName                      NVARCHAR(50)      NOT NULL,
    Username                      NVARCHAR(50)      NOT NULL,
    Email                         NVARCHAR(100)     NOT NULL,
    PasswordHash                  NVARCHAR(255)     NOT NULL,
    AvatarUrl                     NVARCHAR(300)     NULL,
    Bio                           NVARCHAR(300)     NULL,
    RefreshTokenHash              NVARCHAR(128)     NULL,
    RefreshTokenExpiresAt         DATETIMEOFFSET    NULL,
    PreviousRefreshTokenHash      NVARCHAR(128)     NULL,
    PreviousRefreshTokenExpiresAt DATETIMEOFFSET    NULL,
    LastLoginAt                   DATETIMEOFFSET    NULL,
    IsActive                      BIT               NOT NULL CONSTRAINT DF_Reader_IsActive DEFAULT 1,
    CreatedAt                     DATETIMEOFFSET    NOT NULL CONSTRAINT DF_Reader_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt                     DATETIMEOFFSET    NULL,
    IsDeleted                     BIT               NOT NULL CONSTRAINT DF_Reader_IsDeleted DEFAULT 0,
    DeletedAt                     DATETIMEOFFSET    NULL,
    DeletedByAdminId              VARCHAR(26)       NULL CONSTRAINT FK_Reader_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Reader_Username_NotDeleted     ON dbo.Tbl_Reader(Username)                 WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Reader_Email_NotDeleted        ON dbo.Tbl_Reader(Email)                    WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Reader_RefreshTokenHash         ON dbo.Tbl_Reader(RefreshTokenHash)         WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
CREATE NONCLUSTERED INDEX        IX_Tbl_Reader_PreviousRefreshTokenHash ON dbo.Tbl_Reader(PreviousRefreshTokenHash) WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
GO

-- ----------------------------------------------------------------------------
-- 12. Tbl_ReaderBookmark
-- ----------------------------------------------------------------------------
CREATE TABLE dbo.Tbl_ReaderBookmark (
    ReaderBookmarkId VARCHAR(26)       NOT NULL PRIMARY KEY,
    ReaderId         VARCHAR(26)       NOT NULL CONSTRAINT FK_ReaderBookmark_Reader REFERENCES dbo.Tbl_Reader(ReaderId) ON DELETE CASCADE,
    ArticleId        VARCHAR(26)       NOT NULL CONSTRAINT FK_ReaderBookmark_Article REFERENCES dbo.Tbl_Article(ArticleId) ON DELETE CASCADE,
    CreatedAt        DATETIMEOFFSET    NOT NULL CONSTRAINT DF_ReaderBookmark_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_ReaderBookmark_Reader_Article UNIQUE NONCLUSTERED (ReaderId, ArticleId)
);
GO

CREATE NONCLUSTERED INDEX IX_Tbl_ReaderBookmark_ReaderId_CreatedAt ON dbo.Tbl_ReaderBookmark (ReaderId, CreatedAt DESC);
CREATE NONCLUSTERED INDEX IX_Tbl_ReaderBookmark_ArticleId          ON dbo.Tbl_ReaderBookmark (ArticleId);
GO


-- ============================================================================
-- 3. STORED PROCEDURES (T-SQL for Dapper Integration - ULID Parameters)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. ARTICLE CATEGORIES
-- ----------------------------------------------------------------------------

-- Procedure: fn_get_article_category_list
CREATE OR ALTER PROCEDURE dbo.fn_get_article_category_list
    @p_search_keyword NVARCHAR(100) = NULL,
    @p_is_active      BIT           = NULL,
    @p_page           INT           = 1,
    @p_page_size      INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @v_total BIGINT;

    SELECT @v_total = COUNT(1)
    FROM dbo.Tbl_ArticleCategory c
    WHERE c.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR c.[Name] LIKE N'%' + @p_search_keyword + N'%');

    SELECT 
        @v_total AS TotalCount,
        c.ArticleCategoryId,
        c.[Name],
        c.Slug,
        c.[Description],
        c.SortOrder,
        c.CreatedAt,
        c.UpdatedAt
    FROM dbo.Tbl_ArticleCategory c
    WHERE c.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR c.[Name] LIKE N'%' + @p_search_keyword + N'%')
    ORDER BY c.SortOrder ASC, c.CreatedAt ASC
    OFFSET (@p_page - 1) * @p_page_size ROWS
    FETCH NEXT @p_page_size ROWS ONLY;
END;
GO

-- Procedure: fn_get_article_category_by_id
CREATE OR ALTER PROCEDURE dbo.fn_get_article_category_by_id
    @p_category_id VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.ArticleCategoryId,
        c.[Name],
        c.Slug,
        c.[Description],
        c.SortOrder,
        c.CreatedAt,
        c.UpdatedAt
    FROM dbo.Tbl_ArticleCategory c
    WHERE c.ArticleCategoryId = @p_category_id
      AND c.IsDeleted = 0;
END;
GO

-- Procedure: fn_get_article_category_by_slug
CREATE OR ALTER PROCEDURE dbo.fn_get_article_category_by_slug
    @p_slug NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.ArticleCategoryId,
        c.[Name],
        c.Slug,
        c.[Description],
        c.SortOrder,
        c.CreatedAt,
        c.UpdatedAt
    FROM dbo.Tbl_ArticleCategory c
    WHERE c.Slug = @p_slug
      AND c.IsDeleted = 0;
END;
GO

-- ----------------------------------------------------------------------------
-- 2. AUTHORS
-- ----------------------------------------------------------------------------

-- Procedure: fn_get_author_list
CREATE OR ALTER PROCEDURE dbo.fn_get_author_list
    @p_search_keyword NVARCHAR(100) = NULL,
    @p_is_approved    BIT           = NULL,
    @p_is_active      BIT           = NULL,
    @p_page           INT           = 1,
    @p_page_size      INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @v_total BIGINT;

    SELECT @v_total = COUNT(1)
    FROM dbo.Tbl_Author a
    WHERE a.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR a.FirstName LIKE N'%' + @p_search_keyword + N'%' OR a.LastName LIKE N'%' + @p_search_keyword + N'%' OR a.Email LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_is_approved IS NULL OR a.IsApproved = @p_is_approved)
      AND (@p_is_active IS NULL OR a.IsActive = @p_is_active);

    SELECT 
        @v_total AS TotalCount,
        a.AuthorId,
        a.FirstName,
        a.LastName,
        a.Email,
        a.Title,
        a.Bio,
        a.PhotoUrl,
        a.InstagramUrl,
        a.TwitterUrl,
        a.WebsiteUrl,
        a.Slug,
        a.IsApproved,
        a.IsActive,
        a.CreatedAt,
        a.UpdatedAt
    FROM dbo.Tbl_Author a
    WHERE a.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR a.FirstName LIKE N'%' + @p_search_keyword + N'%' OR a.LastName LIKE N'%' + @p_search_keyword + N'%' OR a.Email LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_is_approved IS NULL OR a.IsApproved = @p_is_approved)
      AND (@p_is_active IS NULL OR a.IsActive = @p_is_active)
    ORDER BY a.CreatedAt DESC
    OFFSET (@p_page - 1) * @p_page_size ROWS
    FETCH NEXT @p_page_size ROWS ONLY;
END;
GO

-- Procedure: fn_get_author_by_id
CREATE OR ALTER PROCEDURE dbo.fn_get_author_by_id
    @p_author_id VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        a.AuthorId,
        a.FirstName,
        a.LastName,
        a.Email,
        a.Title,
        a.Bio,
        a.PhotoUrl,
        a.InstagramUrl,
        a.TwitterUrl,
        a.WebsiteUrl,
        a.Slug,
        a.IsApproved,
        a.IsActive,
        a.CreatedAt,
        a.UpdatedAt
    FROM dbo.Tbl_Author a
    WHERE a.AuthorId = @p_author_id
      AND a.IsDeleted = 0;
END;
GO

-- ----------------------------------------------------------------------------
-- 3. ARTICLES
-- ----------------------------------------------------------------------------

-- Procedure: fn_get_article_list
CREATE OR ALTER PROCEDURE dbo.fn_get_article_list
    @p_search_keyword NVARCHAR(200) = NULL,
    @p_category_id    VARCHAR(26)   = NULL,
    @p_author_id      VARCHAR(26)   = NULL,
    @p_status         NVARCHAR(20)  = NULL,
    @p_is_featured    BIT           = NULL,
    @p_is_spotlight   BIT           = NULL,
    @p_page           INT           = 1,
    @p_page_size      INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @v_total BIGINT;

    SELECT @v_total = COUNT(1)
    FROM dbo.Tbl_Article ar
    WHERE ar.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR ar.Title LIKE N'%' + @p_search_keyword + N'%' OR ar.SubTitle LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_category_id IS NULL OR @p_category_id = '' OR ar.ArticleCategoryId = @p_category_id)
      AND (@p_author_id IS NULL OR @p_author_id = '' OR ar.AuthorId = @p_author_id)
      AND (@p_status IS NULL OR @p_status = N'' OR ar.[Status] = @p_status)
      AND (@p_is_featured IS NULL OR ar.IsFeatured = @p_is_featured)
      AND (@p_is_spotlight IS NULL OR ar.IsSpotlight = @p_is_spotlight);

    SELECT 
        @v_total AS TotalCount,
        ar.ArticleId,
        ar.Title,
        ar.SubTitle,
        ar.Slug,
        ar.Body,
        ar.PhotoUrl,
        ar.PhotoCaption,
        ar.PhotoCredit,
        ar.ArticleCategoryId,
        c.[Name] AS CategoryName,
        ar.AuthorId,
        CONCAT(a.FirstName, N' ', a.LastName) AS AuthorName,
        ar.[Status],
        ar.PublishedAt,
        ar.IsFeatured,
        ar.IsSpotlight,
        ar.CreatedAt,
        ar.UpdatedAt
    FROM dbo.Tbl_Article ar
    INNER JOIN dbo.Tbl_ArticleCategory c ON ar.ArticleCategoryId = c.ArticleCategoryId
    INNER JOIN dbo.Tbl_Author a ON ar.AuthorId = a.AuthorId
    WHERE ar.IsDeleted = 0
      AND (@p_search_keyword IS NULL OR ar.Title LIKE N'%' + @p_search_keyword + N'%' OR ar.SubTitle LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_category_id IS NULL OR @p_category_id = '' OR ar.ArticleCategoryId = @p_category_id)
      AND (@p_author_id IS NULL OR @p_author_id = '' OR ar.AuthorId = @p_author_id)
      AND (@p_status IS NULL OR @p_status = N'' OR ar.[Status] = @p_status)
      AND (@p_is_featured IS NULL OR ar.IsFeatured = @p_is_featured)
      AND (@p_is_spotlight IS NULL OR ar.IsSpotlight = @p_is_spotlight)
    ORDER BY COALESCE(ar.PublishedAt, ar.CreatedAt) DESC
    OFFSET (@p_page - 1) * @p_page_size ROWS
    FETCH NEXT @p_page_size ROWS ONLY;
END;
GO

-- Procedure: fn_get_article_by_id
CREATE OR ALTER PROCEDURE dbo.fn_get_article_by_id
    @p_article_id VARCHAR(26),
    @p_author_id  VARCHAR(26) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ar.ArticleId,
        ar.Title,
        ar.SubTitle,
        ar.Slug,
        ar.Body,
        ar.PhotoUrl,
        ar.PhotoCaption,
        ar.PhotoCredit,
        ar.ArticleCategoryId,
        c.[Name] AS CategoryName,
        ar.AuthorId,
        CONCAT(a.FirstName, N' ', a.LastName) AS AuthorName,
        ar.[Status],
        ar.PublishedAt,
        ar.IsFeatured,
        ar.IsSpotlight,
        ar.CreatedAt,
        ar.UpdatedAt
    FROM dbo.Tbl_Article ar
    INNER JOIN dbo.Tbl_ArticleCategory c ON ar.ArticleCategoryId = c.ArticleCategoryId
    INNER JOIN dbo.Tbl_Author a ON ar.AuthorId = a.AuthorId
    WHERE ar.ArticleId = @p_article_id
      AND ar.IsDeleted = 0
      AND (@p_author_id IS NULL OR @p_author_id = '' OR ar.AuthorId = @p_author_id);
END;
GO

-- Procedure: fn_get_article_by_slug
CREATE OR ALTER PROCEDURE dbo.fn_get_article_by_slug
    @p_slug NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ar.ArticleId,
        ar.Title,
        ar.SubTitle,
        ar.Slug,
        ar.Body,
        ar.PhotoUrl,
        ar.PhotoCaption,
        ar.PhotoCredit,
        ar.ArticleCategoryId,
        c.[Name] AS CategoryName,
        ar.AuthorId,
        CONCAT(a.FirstName, N' ', a.LastName) AS AuthorName,
        ar.[Status],
        ar.PublishedAt,
        ar.IsFeatured,
        ar.IsSpotlight,
        ar.CreatedAt,
        ar.UpdatedAt
    FROM dbo.Tbl_Article ar
    INNER JOIN dbo.Tbl_ArticleCategory c ON ar.ArticleCategoryId = c.ArticleCategoryId
    INNER JOIN dbo.Tbl_Author a ON ar.AuthorId = a.AuthorId
    WHERE ar.Slug = @p_slug
      AND ar.IsDeleted = 0;
END;
GO

-- Procedure: fn_get_article_photos_by_article_id
CREATE OR ALTER PROCEDURE dbo.fn_get_article_photos_by_article_id
    @p_article_id VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.ArticlePhotoId,
        ap.ArticleId,
        ap.RequestArticleId,
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

-- Procedure: fn_get_article_photos_by_request_id
CREATE OR ALTER PROCEDURE dbo.fn_get_article_photos_by_request_id
    @p_request_id VARCHAR(26)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.ArticlePhotoId,
        ap.ArticleId,
        ap.RequestArticleId,
        ap.PhotoUrl,
        ap.Caption,
        ap.Credit,
        ap.SortOrder,
        ap.CreatedAt
    FROM dbo.Tbl_ArticlePhoto ap
    WHERE ap.RequestArticleId = @p_request_id
      AND ap.IsDeleted = 0
    ORDER BY ap.SortOrder ASC, ap.CreatedAt ASC;
END;
GO

-- ----------------------------------------------------------------------------
-- 4. REQUEST ARTICLES (AUTHOR STAGING & ADMIN APPROVAL)
-- ----------------------------------------------------------------------------

-- Procedure: fn_get_request_article_list
CREATE OR ALTER PROCEDURE dbo.fn_get_request_article_list
    @p_search_keyword NVARCHAR(200) = NULL,
    @p_category_id    VARCHAR(26)   = NULL,
    @p_author_id      VARCHAR(26)   = NULL,
    @p_status         NVARCHAR(20)  = NULL,
    @p_exclude_draft  BIT           = 0,
    @p_page           INT           = 1,
    @p_page_size      INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @v_total BIGINT;

    SELECT @v_total = COUNT(1)
    FROM dbo.Tbl_RequestArticle r
    WHERE r.IsDeleted = 0
      AND (@p_exclude_draft = 0 OR r.[Status] <> N'Draft')
      AND (@p_search_keyword IS NULL OR r.Title LIKE N'%' + @p_search_keyword + N'%' OR r.SubTitle LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_category_id IS NULL OR @p_category_id = '' OR r.ArticleCategoryId = @p_category_id)
      AND (@p_author_id IS NULL OR @p_author_id = '' OR r.AuthorId = @p_author_id)
      AND (@p_status IS NULL OR @p_status = N'' OR r.[Status] = @p_status);

    SELECT 
        @v_total AS TotalCount,
        r.RequestArticleId,
        r.Title,
        r.SubTitle,
        r.Slug,
        r.Body,
        r.PhotoUrl,
        r.PhotoCaption,
        r.PhotoCredit,
        r.ArticleCategoryId,
        c.[Name] AS CategoryName,
        r.AuthorId,
        CONCAT(a.FirstName, N' ', a.LastName) AS AuthorName,
        r.[Status],
        r.SubmittedAt,
        r.ReviewedAt,
        r.ReviewedByAdminId,
        r.AdminNotes,
        r.PublishedArticleId,
        r.CreatedAt,
        r.UpdatedAt
    FROM dbo.Tbl_RequestArticle r
    INNER JOIN dbo.Tbl_ArticleCategory c ON r.ArticleCategoryId = c.ArticleCategoryId
    INNER JOIN dbo.Tbl_Author a ON r.AuthorId = a.AuthorId
    WHERE r.IsDeleted = 0
      AND (@p_exclude_draft = 0 OR r.[Status] <> N'Draft')
      AND (@p_search_keyword IS NULL OR r.Title LIKE N'%' + @p_search_keyword + N'%' OR r.SubTitle LIKE N'%' + @p_search_keyword + N'%')
      AND (@p_category_id IS NULL OR @p_category_id = '' OR r.ArticleCategoryId = @p_category_id)
      AND (@p_author_id IS NULL OR @p_author_id = '' OR r.AuthorId = @p_author_id)
      AND (@p_status IS NULL OR @p_status = N'' OR r.[Status] = @p_status)
    ORDER BY COALESCE(r.SubmittedAt, r.CreatedAt) DESC
    OFFSET (@p_page - 1) * @p_page_size ROWS
    FETCH NEXT @p_page_size ROWS ONLY;
END;
GO

-- Procedure: fn_get_request_article_by_id
CREATE OR ALTER PROCEDURE dbo.fn_get_request_article_by_id
    @p_request_id    VARCHAR(26),
    @p_author_id     VARCHAR(26) = NULL,
    @p_exclude_draft BIT         = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        r.RequestArticleId,
        r.Title,
        r.SubTitle,
        r.Slug,
        r.Body,
        r.PhotoUrl,
        r.PhotoCaption,
        r.PhotoCredit,
        r.ArticleCategoryId,
        c.[Name] AS CategoryName,
        r.AuthorId,
        CONCAT(a.FirstName, N' ', a.LastName) AS AuthorName,
        r.[Status],
        r.SubmittedAt,
        r.ReviewedAt,
        r.ReviewedByAdminId,
        r.AdminNotes,
        r.PublishedArticleId,
        r.CreatedAt,
        r.UpdatedAt
    FROM dbo.Tbl_RequestArticle r
    INNER JOIN dbo.Tbl_ArticleCategory c ON r.ArticleCategoryId = c.ArticleCategoryId
    INNER JOIN dbo.Tbl_Author a ON r.AuthorId = a.AuthorId
    WHERE r.RequestArticleId = @p_request_id
      AND r.IsDeleted = 0
      AND (@p_exclude_draft = 0 OR r.[Status] <> N'Draft')
      AND (@p_author_id IS NULL OR @p_author_id = '' OR r.AuthorId = @p_author_id);
END;
GO


-- ============================================================================
-- 4. INITIAL SEED DATA (With 26-Character ULIDs)
-- ============================================================================

-- Seed 1: Initial SuperAdmin User (Password: Admin@123)
-- BCrypt work-factor 12 hash matching Magizine.Shared.Security.PasswordHasher
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Admin WHERE Username = 'superadmin')
BEGIN
    INSERT INTO dbo.Tbl_Admin (AdminId, Username, Email, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        '01JADMIN000000000000000001',
        'superadmin',
        'admin@ledition.com',
        '$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.',
        'SuperAdmin',
        1,
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Seed 2: Default Article Categories
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ArticleCategory WHERE Slug = 'beauty')
BEGIN
    INSERT INTO dbo.Tbl_ArticleCategory (ArticleCategoryId, Name, Slug, Description, SortOrder, CreatedAt)
    VALUES 
        ('01JCAT00000000000000000001', N'Beauty', N'beauty', N'Skincare, makeup, and wellness routines curated for the modern minimalist.', 1, SYSDATETIMEOFFSET()),
        ('01JCAT00000000000000000002', N'Health Care', N'health-care', N'Holistic health, nutrition, and balanced lifestyle essays.', 2, SYSDATETIMEOFFSET()),
        ('01JCAT00000000000000000003', N'Musing', N'musing', N'Reflections on culture, literature, design, and thoughtful living.', 3, SYSDATETIMEOFFSET());
END;
GO

-- Seed 3: Standard App Settings in Tbl_AboutAndAppData
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_AboutAndAppData WHERE [Key] = 'SiteMission')
BEGIN
    INSERT INTO dbo.Tbl_AboutAndAppData (AppDataId, [Key], [Value], GroupName, CreatedAt)
    VALUES
        ('01JDATA0000000000000000001', N'SiteMission', N'L’ÉDITION is an independent digital magazine exploring quiet luxury, intentional aesthetics, and literature.', N'About', SYSDATETIMEOFFSET()),
        ('01JDATA0000000000000000002', N'EditorialPolicy', N'All submitted articles undergo careful editorial review before publication.', N'Legal', SYSDATETIMEOFFSET()),
        ('01JDATA0000000000000000003', N'PrivacyPolicy', N'We do not sell personal reader information to third parties.', N'Legal', SYSDATETIMEOFFSET()),
        ('01JDATA0000000000000000004', N'ContactEmail', N'editorial@ledition.com', N'General', SYSDATETIMEOFFSET()),
        ('01JDATA0000000000000000005', N'InstagramUrl', N'https://instagram.com/ledition_mag', N'Social', SYSDATETIMEOFFSET()),
        ('01JDATA0000000000000000006', N'TwitterUrl', N'https://twitter.com/ledition_mag', N'Social', SYSDATETIMEOFFSET());
END;
GO

-- Seed 4: Default Author (Matches AuthorizedAuthorId in MagizineAuthor.Api)
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
        N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.',
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

-- Seed 5: Default Published Article in Tbl_Article
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
    );
END;
GO

-- Seed 6: Default Author Article Requests in Tbl_RequestArticle (1 Submitted, 1 Draft)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_RequestArticle WHERE RequestArticleId = '01JREQ00000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_RequestArticle (
        RequestArticleId, Title, SubTitle, Slug, Body, PhotoUrl, PhotoCaption, PhotoCredit,
        ArticleCategoryId, AuthorId, [Status], SubmittedAt, CreatedAt
    )
    VALUES
    (
        '01JREQ00000000000000000001',
        N'Monastic Architecture in Contemporary Homes',
        N'How Cistercian simplicity inspires serene urban residences',
        N'monastic-architecture-in-contemporary-homes',
        N'<p>An essay examining how contemporary architects draw inspiration from Cistercian monasteries to create serene urban residences.</p>',
        N'https://images.unsplash.com/photo-1513694203232-719a280e022f',
        N'Minimalist limestone courtyard in Kyoto.',
        N'Photo by Kenji Sato',
        '01JCAT00000000000000000003',
        '01JAUTHOR00000000000000001',
        N'Submitted',
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    ),
    (
        '01JREQ00000000000000000002',
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
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Seed 7: Default Article Gallery Photos (Tbl_ArticlePhoto)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ArticlePhoto WHERE ArticlePhotoId = '01JAPHT0000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_ArticlePhoto (
        ArticlePhotoId, ArticleId, RequestArticleId, PhotoUrl, Caption, Credit, SortOrder, CreatedAt
    )
    VALUES
    (
        '01JAPHT0000000000000000001',
        '01JART00000000000000000001',
        NULL,
        N'https://images.unsplash.com/photo-1578749556568-bc2c40e68b61',
        N'Handcrafted stoneware vessels in afternoon shadow.',
        N'Photo by L''ÉDITION Studio',
        1,
        SYSDATETIMEOFFSET()
    ),
    (
        '01JAPHT0000000000000000002',
        NULL,
        '01JREQ00000000000000000001',
        N'https://images.unsplash.com/photo-1513694203232-719a280e022f',
        N'Minimalist limestone courtyard in Kyoto.',
        N'Photo by Kenji Sato',
        1,
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Seed 8: Default Public Reader in Tbl_Reader (sophia.reader / Admin@123)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Reader WHERE ReaderId = '01JREADER00000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_Reader (
        ReaderId, FirstName, LastName, Username, Email, PasswordHash,
        AvatarUrl, Bio, IsActive, CreatedAt
    )
    VALUES
    (
        '01JREADER00000000000000001',
        N'Sophia',
        N'Laurent',
        N'sophia.reader',
        N'sophia.reader@ledition.com',
        N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.',
        N'https://images.unsplash.com/photo-1544005313-94ddf0286df2',
        N'Design enthusiast and avid reader of contemporary architecture and slow living essays.',
        1,
        SYSDATETIMEOFFSET()
    );
END;
GO

-- Seed 9: Default Public Reader Bookmark in Tbl_ReaderBookmark
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ReaderBookmark WHERE ReaderBookmarkId = '01JBKMRK000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_ReaderBookmark (
        ReaderBookmarkId, ReaderId, ArticleId, CreatedAt
    )
    VALUES
    (
        '01JBKMRK000000000000000001',
        '01JREADER00000000000000001',
        '01JART00000000000000000001',
        SYSDATETIMEOFFSET()
    );
END;
GO

