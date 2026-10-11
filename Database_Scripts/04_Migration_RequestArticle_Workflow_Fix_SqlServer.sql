-- ============================================================================
-- L'ÉDITION MAGAZINE PLATFORM - AUTHOR-TO-ADMIN ARTICLE WORKFLOW FIX SCRIPT
-- ============================================================================
-- Target Engine : Microsoft SQL Server 2017+ / Azure SQL Database
-- Database      : MagizineDB
-- Safe Mode     : Idempotent Migration & Fix Script
--
-- What this script does:
--   1. Upgrades dbo.Tbl_RequestArticle from legacy pitch table to full
--      Author-to-Admin staging table (Draft -> Submitted -> Approved / Rejected)
--   2. Updates dbo.Tbl_ArticlePhoto so ArticleId is NULLABLE and adds
--      RequestArticleId (NULLABLE FK) for staging gallery photos prior to approval
--   3. Creates/Updates Dapper Stored Procedures:
--        - dbo.fn_get_request_article_list
--        - dbo.fn_get_request_article_by_id
--        - dbo.fn_get_article_photos_by_article_id
--        - dbo.fn_get_article_photos_by_request_id
--   4. Migrates / Seeds default Author RequestArticle records (Submitted + Draft)
--      and staged gallery photos idempotently
-- ============================================================================

USE MagizineDB;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. UPGRADE TABLE: dbo.Tbl_RequestArticle (Author Staging & Admin Review)
-- ============================================================================

-- If Tbl_RequestArticle still has the old pitch schema (PitcherName column exists
-- or AuthorId column is missing), drop dependent FKs and recreate Tbl_RequestArticle.
IF OBJECT_ID('dbo.Tbl_RequestArticle', 'U') IS NOT NULL
   AND (COL_LENGTH('dbo.Tbl_RequestArticle', 'PitcherName') IS NOT NULL
        OR COL_LENGTH('dbo.Tbl_RequestArticle', 'AuthorId') IS NULL)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tbl_ArticlePhoto_RequestArticle')
    BEGIN
        ALTER TABLE dbo.Tbl_ArticlePhoto DROP CONSTRAINT FK_Tbl_ArticlePhoto_RequestArticle;
    END;

    DROP TABLE dbo.Tbl_RequestArticle;
END;
GO

IF OBJECT_ID('dbo.Tbl_RequestArticle', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_RequestArticle (
        RequestArticleId    VARCHAR(26)        NOT NULL,
        Title               NVARCHAR(250)      NOT NULL,
        SubTitle            NVARCHAR(500)      NULL,
        Slug                NVARCHAR(250)      NOT NULL,
        Body                NVARCHAR(MAX)      NOT NULL,
        PhotoUrl            NVARCHAR(300)      NULL,
        PhotoCaption        NVARCHAR(200)      NULL,
        PhotoCredit         NVARCHAR(100)      NULL,
        ArticleCategoryId   VARCHAR(26)        NOT NULL,
        AuthorId            VARCHAR(26)        NOT NULL,
        [Status]            NVARCHAR(20)       NOT NULL CONSTRAINT DF_Tbl_RequestArticle_Status DEFAULT N'Draft', -- 'Draft' | 'Submitted' | 'Approved' | 'Rejected'
        SubmittedAt         DATETIMEOFFSET(7)  NULL,
        ReviewedAt          DATETIMEOFFSET(7)  NULL,
        ReviewedByAdminId   VARCHAR(26)        NULL,
        AdminNotes          NVARCHAR(1000)     NULL,
        PublishedArticleId  VARCHAR(26)        NULL,
        CreatedAt           DATETIMEOFFSET(7)  NOT NULL CONSTRAINT DF_Tbl_RequestArticle_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt           DATETIMEOFFSET(7)  NULL,
        IsDeleted           BIT                NOT NULL CONSTRAINT DF_Tbl_RequestArticle_IsDeleted DEFAULT 0,
        DeletedAt           DATETIMEOFFSET(7)  NULL,
        DeletedByAdminId    VARCHAR(26)        NULL,

        CONSTRAINT PK_Tbl_RequestArticle PRIMARY KEY CLUSTERED (RequestArticleId ASC),
        CONSTRAINT FK_Tbl_RequestArticle_Category FOREIGN KEY (ArticleCategoryId) REFERENCES dbo.Tbl_ArticleCategory (ArticleCategoryId),
        CONSTRAINT FK_Tbl_RequestArticle_Author FOREIGN KEY (AuthorId) REFERENCES dbo.Tbl_Author (AuthorId),
        CONSTRAINT FK_Tbl_RequestArticle_ReviewedByAdmin FOREIGN KEY (ReviewedByAdminId) REFERENCES dbo.Tbl_Admin (AdminId),
        CONSTRAINT FK_Tbl_RequestArticle_PublishedArticle FOREIGN KEY (PublishedArticleId) REFERENCES dbo.Tbl_Article (ArticleId),
        CONSTRAINT FK_Tbl_RequestArticle_DeletedByAdmin FOREIGN KEY (DeletedByAdminId) REFERENCES dbo.Tbl_Admin (AdminId)
    );
END;
GO

-- Ensure all indexes on dbo.Tbl_RequestArticle exist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_RequestArticle_AuthorId' AND object_id = OBJECT_ID('dbo.Tbl_RequestArticle'))
    CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_AuthorId ON dbo.Tbl_RequestArticle(AuthorId) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_RequestArticle_CategoryId' AND object_id = OBJECT_ID('dbo.Tbl_RequestArticle'))
    CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_CategoryId ON dbo.Tbl_RequestArticle(ArticleCategoryId) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_RequestArticle_Status' AND object_id = OBJECT_ID('dbo.Tbl_RequestArticle'))
    CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_Status ON dbo.Tbl_RequestArticle([Status], CreatedAt DESC) WHERE IsDeleted = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_RequestArticle_Cleanup' AND object_id = OBJECT_ID('dbo.Tbl_RequestArticle'))
    CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_Cleanup ON dbo.Tbl_RequestArticle(IsDeleted, DeletedAt) WHERE IsDeleted = 1;
GO


-- ============================================================================
-- 2. UPGRADE TABLE: dbo.Tbl_ArticlePhoto (Nullable ArticleId + RequestArticleId)
-- ============================================================================

IF OBJECT_ID('dbo.Tbl_ArticlePhoto', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_ArticlePhoto (
        ArticlePhotoId      VARCHAR(26)        NOT NULL,
        ArticleId           VARCHAR(26)        NULL,
        RequestArticleId    VARCHAR(26)        NULL,
        PhotoUrl            NVARCHAR(300)      NOT NULL,
        Caption             NVARCHAR(200)      NULL,
        Credit              NVARCHAR(100)      NULL,
        SortOrder           INT                NOT NULL CONSTRAINT DF_Tbl_ArticlePhoto_SortOrder DEFAULT 0,
        CreatedAt           DATETIMEOFFSET(7)  NOT NULL CONSTRAINT DF_Tbl_ArticlePhoto_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        UpdatedAt           DATETIMEOFFSET(7)  NULL,
        IsDeleted           BIT                NOT NULL CONSTRAINT DF_Tbl_ArticlePhoto_IsDeleted DEFAULT 0,
        DeletedAt           DATETIMEOFFSET(7)  NULL,
        DeletedByAdminId    VARCHAR(26)        NULL,

        CONSTRAINT PK_Tbl_ArticlePhoto PRIMARY KEY CLUSTERED (ArticlePhotoId ASC),
        CONSTRAINT FK_Tbl_ArticlePhoto_Article FOREIGN KEY (ArticleId) REFERENCES dbo.Tbl_Article (ArticleId),
        CONSTRAINT FK_Tbl_ArticlePhoto_RequestArticle FOREIGN KEY (RequestArticleId) REFERENCES dbo.Tbl_RequestArticle (RequestArticleId),
        CONSTRAINT FK_Tbl_ArticlePhoto_DeletedByAdmin FOREIGN KEY (DeletedByAdminId) REFERENCES dbo.Tbl_Admin (AdminId)
    );
END
ELSE
BEGIN
    -- Make ArticleId NULLABLE if it is currently NOT NULL
    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto')
          AND name = 'ArticleId'
          AND is_nullable = 0
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ArticlePhoto_ArticleId' AND object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto'))
            DROP INDEX IX_Tbl_ArticlePhoto_ArticleId ON dbo.Tbl_ArticlePhoto;

        ALTER TABLE dbo.Tbl_ArticlePhoto ALTER COLUMN ArticleId VARCHAR(26) NULL;
    END;

    -- Add RequestArticleId column if missing
    IF COL_LENGTH('dbo.Tbl_ArticlePhoto', 'RequestArticleId') IS NULL
    BEGIN
        ALTER TABLE dbo.Tbl_ArticlePhoto ADD RequestArticleId VARCHAR(26) NULL;
    END;

    -- Add FK_Tbl_ArticlePhoto_RequestArticle constraint if missing
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tbl_ArticlePhoto_RequestArticle')
    BEGIN
        ALTER TABLE dbo.Tbl_ArticlePhoto
        ADD CONSTRAINT FK_Tbl_ArticlePhoto_RequestArticle
        FOREIGN KEY (RequestArticleId) REFERENCES dbo.Tbl_RequestArticle (RequestArticleId);
    END;
END;
GO

-- Ensure indexes on dbo.Tbl_ArticlePhoto
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ArticlePhoto_ArticleId' AND object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_ArticleId
    ON dbo.Tbl_ArticlePhoto(ArticleId, SortOrder ASC)
    WHERE IsDeleted = 0 AND ArticleId IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tbl_ArticlePhoto_RequestArticleId' AND object_id = OBJECT_ID('dbo.Tbl_ArticlePhoto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ArticlePhoto_RequestArticleId
    ON dbo.Tbl_ArticlePhoto(RequestArticleId, SortOrder ASC)
    WHERE IsDeleted = 0 AND RequestArticleId IS NOT NULL;
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
-- 3. CREATE / UPDATE STORED PROCEDURES
-- ============================================================================

-- 3.1 RequestArticle Paginated List (Supports Author scoping & Admin Draft exclusion)
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

-- 3.2 RequestArticle Detail By ID
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

-- 3.3 Article Photos By Published ArticleId
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

-- 3.4 Article Photos By Staged RequestArticleId
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


-- ============================================================================
-- 4. CLEANUP & SEED DATA FOR STAGED REQUEST ARTICLES (Idempotent)
-- ============================================================================

-- Remove legacy Draft row from Tbl_Article if present (Drafts now live in Tbl_RequestArticle)
IF EXISTS (SELECT 1 FROM dbo.Tbl_Article WHERE ArticleId = '01JART00000000000000000002' AND [Status] = N'Draft')
BEGIN
    DELETE FROM dbo.Tbl_ArticlePhoto WHERE ArticleId = '01JART00000000000000000002';
    DELETE FROM dbo.Tbl_Article WHERE ArticleId = '01JART00000000000000000002';
END;
GO

-- Seed Submitted RequestArticle (Visible to both Author and Admin)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_RequestArticle WHERE RequestArticleId = '01JREQ00000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_RequestArticle (
        RequestArticleId, Title, SubTitle, Slug, Body, PhotoUrl, PhotoCaption, PhotoCredit,
        ArticleCategoryId, AuthorId, [Status], SubmittedAt, CreatedAt
    )
    VALUES (
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
    );
END;
GO

-- Seed Draft RequestArticle (Private to Author, hidden from Admin)
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_RequestArticle WHERE RequestArticleId = '01JREQ00000000000000000002')
BEGIN
    INSERT INTO dbo.Tbl_RequestArticle (
        RequestArticleId, Title, SubTitle, Slug, Body, PhotoUrl, PhotoCaption, PhotoCredit,
        ArticleCategoryId, AuthorId, [Status], SubmittedAt, CreatedAt
    )
    VALUES (
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

-- Seed Staged Gallery Photo attached to RequestArticleId = '01JREQ00000000000000000001'
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ArticlePhoto WHERE ArticlePhotoId = '01JAPHT0000000000000000003')
BEGIN
    INSERT INTO dbo.Tbl_ArticlePhoto (
        ArticlePhotoId, ArticleId, RequestArticleId, PhotoUrl, Caption, Credit, SortOrder, CreatedAt
    )
    VALUES (
        '01JAPHT0000000000000000003',
        NULL,
        '01JREQ00000000000000000001',
        N'https://images.unsplash.com/photo-1600585154526-990dced4db0d',
        N'Raw concrete and warm oak timber framing.',
        N'Photo by Kenji Sato',
        1,
        SYSDATETIMEOFFSET()
    );
END;
GO
