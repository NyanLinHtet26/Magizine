-- ============================================================================
-- L'ÉDITION MAGAZINE PLATFORM - SQL SERVER (T-SQL) FULL DATABASE SCRIPT
-- ============================================================================
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- 
-- Contents:
--   1. 10 Tables with IDENTITY, Types, Defaults, and Foreign Keys
--   2. Filtered Unique Indexes (Partial Indexes) for Soft-Delete Tables
--   3. Foreign Key & Query Performance Indexes
--   4. Stored Procedures / Functions
--   5. Initial Seed Data
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Tbl_Admin
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Admin', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Admin;
CREATE TABLE dbo.Tbl_Admin (
    AdminId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    Username          NVARCHAR(50) NOT NULL,
    Email             NVARCHAR(100) NOT NULL,
    PasswordHash      NVARCHAR(255) NOT NULL,
    Role              NVARCHAR(30) NOT NULL CONSTRAINT DF_Admin_Role DEFAULT N'Editor',
    LastLoginAt       DATETIMEOFFSET NULL,
    IsActive          BIT NOT NULL CONSTRAINT DF_Admin_IsActive DEFAULT 1,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Admin_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Admin_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Admin_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Username_NotDeleted ON dbo.Tbl_Admin(Username) WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Admin_Email_NotDeleted ON dbo.Tbl_Admin(Email) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 2. Tbl_Author
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Author', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Author;
CREATE TABLE dbo.Tbl_Author (
    AuthorId          BIGINT IDENTITY(1,1) PRIMARY KEY,
    FirstName         NVARCHAR(50) NOT NULL,
    LastName          NVARCHAR(50) NOT NULL,
    Email             NVARCHAR(100) NOT NULL,
    PasswordHash      NVARCHAR(255) NOT NULL,
    Title             NVARCHAR(100) NULL,
    Bio               NVARCHAR(500) NULL,
    PhotoUrl          NVARCHAR(300) NULL,
    InstagramUrl      NVARCHAR(300) NULL,
    TwitterUrl        NVARCHAR(300) NULL,
    WebsiteUrl        NVARCHAR(300) NULL,
    Slug              NVARCHAR(100) NOT NULL,
    IsApproved        BIT NOT NULL CONSTRAINT DF_Author_IsApproved DEFAULT 0,
    IsActive          BIT NOT NULL CONSTRAINT DF_Author_IsActive DEFAULT 1,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Author_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Author_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Author_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Email_NotDeleted ON dbo.Tbl_Author(Email) WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Author_Slug_NotDeleted ON dbo.Tbl_Author(Slug) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 3. Tbl_ArticleCategory
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_ArticleCategory', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ArticleCategory;
CREATE TABLE dbo.Tbl_ArticleCategory (
    ArticleCategoryId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(20) NOT NULL,
    Slug              NVARCHAR(30) NOT NULL,
    Description       NVARCHAR(200) NULL,
    SortOrder         INT NOT NULL CONSTRAINT DF_ArticleCat_SortOrder DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_ArticleCat_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_ArticleCat_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_ArticleCat_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Name_NotDeleted ON dbo.Tbl_ArticleCategory(Name) WHERE IsDeleted = 0;
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_ArticleCategory_Slug_NotDeleted ON dbo.Tbl_ArticleCategory(Slug) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 4. Tbl_Article
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Article', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Article;
CREATE TABLE dbo.Tbl_Article (
    ArticleId         BIGINT IDENTITY(1,1) PRIMARY KEY,
    Title             NVARCHAR(200) NOT NULL,
    SubTitle          NVARCHAR(400) NULL,
    Slug              NVARCHAR(200) NOT NULL,
    Body              NVARCHAR(MAX) NULL,
    PhotoUrl          NVARCHAR(300) NULL,
    PhotoCaption      NVARCHAR(200) NULL,
    PhotoCredit       NVARCHAR(100) NULL,
    ArticleCategoryId BIGINT NOT NULL CONSTRAINT FK_Article_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    AuthorId          BIGINT NOT NULL CONSTRAINT FK_Article_Author REFERENCES dbo.Tbl_Author(AuthorId),
    Status            NVARCHAR(20) NOT NULL CONSTRAINT DF_Article_Status DEFAULT N'Draft',
    PublishedAt       DATETIMEOFFSET NULL,
    IsFeatured        BIT NOT NULL CONSTRAINT DF_Article_IsFeatured DEFAULT 0,
    IsSpotlight       BIT NOT NULL CONSTRAINT DF_Article_IsSpotlight DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Article_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Article_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Article_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Article_Slug_NotDeleted ON dbo.Tbl_Article(Slug) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_Article_CategoryId ON dbo.Tbl_Article(ArticleCategoryId);
CREATE NONCLUSTERED INDEX IX_Tbl_Article_AuthorId ON dbo.Tbl_Article(AuthorId);
CREATE NONCLUSTERED INDEX IX_Tbl_Article_Status ON dbo.Tbl_Article(Status, PublishedAt DESC) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 5. Tbl_RequestArticle
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_RequestArticle', 'U') IS NOT NULL DROP TABLE dbo.Tbl_RequestArticle;
CREATE TABLE dbo.Tbl_RequestArticle (
    RequestArticleId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    PitcherName       NVARCHAR(100) NOT NULL,
    PitcherEmail      NVARCHAR(100) NOT NULL,
    ProposedTitle     NVARCHAR(200) NULL,
    PitchBody         NVARCHAR(MAX) NOT NULL,
    ArticleCategoryId BIGINT NULL CONSTRAINT FK_RequestArticle_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    Status            NVARCHAR(20) NOT NULL CONSTRAINT DF_RequestArticle_Status DEFAULT N'Pending',
    SubmittedAt       DATETIMEOFFSET NOT NULL CONSTRAINT DF_RequestArticle_SubmittedAt DEFAULT SYSDATETIMEOFFSET(),
    ReviewedAt        DATETIMEOFFSET NULL,
    ReviewedByAdminId BIGINT NULL CONSTRAINT FK_RequestArticle_ReviewedByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    AdminNotes        NVARCHAR(500) NULL,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_RequestArticle_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_RequestArticle_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_RequestArticle_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE NONCLUSTERED INDEX IX_Tbl_RequestArticle_Status ON dbo.Tbl_RequestArticle(Status) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 6. Tbl_ContactMessage
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_ContactMessage', 'U') IS NOT NULL DROP TABLE dbo.Tbl_ContactMessage;
CREATE TABLE dbo.Tbl_ContactMessage (
    ContactMessageId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    SenderName        NVARCHAR(100) NOT NULL,
    SenderEmail       NVARCHAR(100) NOT NULL,
    Subject           NVARCHAR(200) NULL,
    MessageBody       NVARCHAR(MAX) NOT NULL,
    IsRead            BIT NOT NULL CONSTRAINT DF_ContactMessage_IsRead DEFAULT 0,
    ReadAt            DATETIMEOFFSET NULL,
    ReadByAdminId     BIGINT NULL CONSTRAINT FK_ContactMessage_ReadByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_ContactMessage_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_ContactMessage_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_ContactMessage_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE NONCLUSTERED INDEX IX_Tbl_ContactMessage_IsRead ON dbo.Tbl_ContactMessage(IsRead) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 7. Tbl_Newsletter
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Newsletter', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Newsletter;
CREATE TABLE dbo.Tbl_Newsletter (
    NewsletterId      BIGINT IDENTITY(1,1) PRIMARY KEY,
    Email             NVARCHAR(100) NOT NULL,
    FullName          NVARCHAR(100) NULL,
    SubscribedAt      DATETIMEOFFSET NOT NULL CONSTRAINT DF_Newsletter_SubscribedAt DEFAULT SYSDATETIMEOFFSET(),
    IsActive          BIT NOT NULL CONSTRAINT DF_Newsletter_IsActive DEFAULT 1,
    UnsubscribedAt    DATETIMEOFFSET NULL,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Newsletter_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Newsletter_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Newsletter_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Newsletter_Email_NotDeleted ON dbo.Tbl_Newsletter(Email) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 8. Tbl_AboutAndAppData (NO Soft-Delete Columns)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_AboutAndAppData', 'U') IS NOT NULL DROP TABLE dbo.Tbl_AboutAndAppData;
CREATE TABLE dbo.Tbl_AboutAndAppData (
    AppDataId         BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Key]             NVARCHAR(100) NOT NULL CONSTRAINT UQ_AboutAndAppData_Key UNIQUE,
    [Value]           NVARCHAR(MAX) NULL,
    GroupName         NVARCHAR(50) NULL,
    UpdatedByAdminId  BIGINT NULL CONSTRAINT FK_AboutAndAppData_UpdatedByAdmin REFERENCES dbo.Tbl_Admin(AdminId),
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_AboutAndAppData_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL
);

-- ----------------------------------------------------------------------------
-- 9. Tbl_Notification
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Notification', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Notification;
CREATE TABLE dbo.Tbl_Notification (
    NotificationId    BIGINT IDENTITY(1,1) PRIMARY KEY,
    RecipientAuthorId BIGINT NULL CONSTRAINT FK_Notification_Author REFERENCES dbo.Tbl_Author(AuthorId),
    RecipientAdminId  BIGINT NULL CONSTRAINT FK_Notification_Admin REFERENCES dbo.Tbl_Admin(AdminId),
    Title             NVARCHAR(150) NOT NULL,
    Message           NVARCHAR(500) NOT NULL,
    Type              NVARCHAR(30) NOT NULL,
    IsRead            BIT NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT 0,
    ReadAt            DATETIMEOFFSET NULL,
    RelatedEntityType NVARCHAR(50) NULL,
    RelatedEntityId   BIGINT NULL,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Notification_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Notification_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Author ON dbo.Tbl_Notification(RecipientAuthorId, IsRead) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Tbl_Notification_Admin  ON dbo.Tbl_Notification(RecipientAdminId, IsRead)  WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- 10. Tbl_Ads
-- ----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Tbl_Ads', 'U') IS NOT NULL DROP TABLE dbo.Tbl_Ads;
CREATE TABLE dbo.Tbl_Ads (
    AdId              BIGINT IDENTITY(1,1) PRIMARY KEY,
    Title             NVARCHAR(150) NOT NULL,
    ImageUrl          NVARCHAR(300) NOT NULL,
    TargetUrl         NVARCHAR(300) NULL,
    Placement         NVARCHAR(50) NOT NULL,
    ArticleCategoryId BIGINT NULL CONSTRAINT FK_Ads_Category REFERENCES dbo.Tbl_ArticleCategory(ArticleCategoryId),
    StartDate         DATETIMEOFFSET NULL,
    EndDate           DATETIMEOFFSET NULL,
    IsActive          BIT NOT NULL CONSTRAINT DF_Ads_IsActive DEFAULT 1,
    ClickCount        INT NOT NULL CONSTRAINT DF_Ads_ClickCount DEFAULT 0,
    ImpressionCount   INT NOT NULL CONSTRAINT DF_Ads_ImpressionCount DEFAULT 0,
    CreatedAt         DATETIMEOFFSET NOT NULL CONSTRAINT DF_Ads_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
    UpdatedAt         DATETIMEOFFSET NULL,
    IsDeleted         BIT NOT NULL CONSTRAINT DF_Ads_IsDeleted DEFAULT 0,
    DeletedAt         DATETIMEOFFSET NULL,
    DeletedByAdminId  BIGINT NULL CONSTRAINT FK_Ads_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
);
CREATE NONCLUSTERED INDEX IX_Tbl_Ads_Placement ON dbo.Tbl_Ads(Placement, IsActive) WHERE IsDeleted = 0;

-- ----------------------------------------------------------------------------
-- SEED DATA
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Admin WHERE Username = 'superadmin')
BEGIN
    INSERT INTO dbo.Tbl_Admin (Username, Email, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        'superadmin',
        'admin@ledition.com',
        '$2a$12$e0NRiA4b2VwYQcT7X8mOVuYqHhFhR8rUj2V0Wb8Z0XlK1mG5o6Pfa',
        'SuperAdmin',
        1,
        SYSDATETIMEOFFSET()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ArticleCategory WHERE Slug = 'beauty')
BEGIN
    INSERT INTO dbo.Tbl_ArticleCategory (Name, Slug, Description, SortOrder, CreatedAt)
    VALUES 
        ('Beauty', 'beauty', 'Skincare, makeup, and wellness routines curated for the modern minimalist.', 1, SYSDATETIMEOFFSET()),
        ('Health Care', 'health-care', 'Holistic health, nutrition, and balanced lifestyle essays.', 2, SYSDATETIMEOFFSET()),
        ('Musing', 'musing', 'Reflections on culture, literature, design, and thoughtful living.', 3, SYSDATETIMEOFFSET());
END;
