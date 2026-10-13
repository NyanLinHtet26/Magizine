-- ============================================================================
-- MIGRATION 08: PUBLIC READER ARTICLE BOOKMARKS
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- Safe to run multiple times (Idempotent)
-- ============================================================================

USE MagizineDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. CREATE TABLE dbo.Tbl_ReaderBookmark IF NOT EXISTS
-- ============================================================================

IF OBJECT_ID('dbo.Tbl_ReaderBookmark', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_ReaderBookmark (
        ReaderBookmarkId VARCHAR(26)       NOT NULL PRIMARY KEY,
        ReaderId         VARCHAR(26)       NOT NULL CONSTRAINT FK_ReaderBookmark_Reader REFERENCES dbo.Tbl_Reader(ReaderId) ON DELETE CASCADE,
        ArticleId        VARCHAR(26)       NOT NULL CONSTRAINT FK_ReaderBookmark_Article REFERENCES dbo.Tbl_Article(ArticleId) ON DELETE CASCADE,
        CreatedAt        DATETIMEOFFSET    NOT NULL CONSTRAINT DF_ReaderBookmark_CreatedAt DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT UQ_ReaderBookmark_Reader_Article UNIQUE NONCLUSTERED (ReaderId, ArticleId)
    );
    PRINT 'Created table dbo.Tbl_ReaderBookmark.';
END;
GO

-- ============================================================================
-- 2. CREATE PERFORMANCE INDEXES
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_ReaderBookmark_ReaderId_CreatedAt'
      AND object_id = OBJECT_ID('dbo.Tbl_ReaderBookmark')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ReaderBookmark_ReaderId_CreatedAt
    ON dbo.Tbl_ReaderBookmark (ReaderId, CreatedAt DESC);
    PRINT 'Created index IX_Tbl_ReaderBookmark_ReaderId_CreatedAt on dbo.Tbl_ReaderBookmark.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_ReaderBookmark_ArticleId'
      AND object_id = OBJECT_ID('dbo.Tbl_ReaderBookmark')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_ReaderBookmark_ArticleId
    ON dbo.Tbl_ReaderBookmark (ArticleId);
    PRINT 'Created index IX_Tbl_ReaderBookmark_ArticleId on dbo.Tbl_ReaderBookmark.';
END;
GO

-- ============================================================================
-- 3. SEED DEFAULT BOOKMARK (sophia.reader bookmarks "The Art of Quiet Luxury")
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_ReaderBookmark WHERE ReaderBookmarkId = '01JBKMRK000000000000000001')
   AND EXISTS (SELECT 1 FROM dbo.Tbl_Reader WHERE ReaderId = '01JREADER00000000000000001')
   AND EXISTS (SELECT 1 FROM dbo.Tbl_Article WHERE ArticleId = '01JART00000000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_ReaderBookmark (
        ReaderBookmarkId, ReaderId, ArticleId, CreatedAt
    )
    VALUES (
        '01JBKMRK000000000000000001',
        '01JREADER00000000000000001',
        '01JART00000000000000000001',
        SYSDATETIMEOFFSET()
    );
    PRINT 'Inserted default seed bookmark for sophia.reader.';
END;
GO
