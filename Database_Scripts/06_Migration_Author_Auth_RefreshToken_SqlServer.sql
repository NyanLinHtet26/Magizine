-- ============================================================================
-- MIGRATION 06: AUTHOR AUTHENTICATION, ROTATING REFRESH TOKEN & 30s GRACE PERIOD
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- Safe to run multiple times (Idempotent)
-- ============================================================================

USE MagizineDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. ADD REFRESH TOKEN, 30-SECOND GRACE PERIOD & LAST LOGIN COLUMNS TO dbo.Tbl_Author
-- ============================================================================

IF COL_LENGTH('dbo.Tbl_Author', 'RefreshTokenHash') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ADD RefreshTokenHash NVARCHAR(128) NULL;
    PRINT 'Added column RefreshTokenHash to dbo.Tbl_Author.';
END;

IF COL_LENGTH('dbo.Tbl_Author', 'RefreshTokenExpiresAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ADD RefreshTokenExpiresAt DATETIMEOFFSET NULL;
    PRINT 'Added column RefreshTokenExpiresAt to dbo.Tbl_Author.';
END;

IF COL_LENGTH('dbo.Tbl_Author', 'PreviousRefreshTokenHash') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ADD PreviousRefreshTokenHash NVARCHAR(128) NULL;
    PRINT 'Added column PreviousRefreshTokenHash to dbo.Tbl_Author.';
END;

IF COL_LENGTH('dbo.Tbl_Author', 'PreviousRefreshTokenExpiresAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ADD PreviousRefreshTokenExpiresAt DATETIMEOFFSET NULL;
    PRINT 'Added column PreviousRefreshTokenExpiresAt to dbo.Tbl_Author.';
END;

IF COL_LENGTH('dbo.Tbl_Author', 'LastLoginAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Author ADD LastLoginAt DATETIMEOFFSET NULL;
    PRINT 'Added column LastLoginAt to dbo.Tbl_Author.';
END;
GO

-- ============================================================================
-- 2. CREATE FILTERED INDEXES FOR FAST O(1) AUTHOR REFRESH TOKEN LOOKUP
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Author_RefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Author')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Author_RefreshTokenHash
    ON dbo.Tbl_Author(RefreshTokenHash)
    WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Author_RefreshTokenHash on dbo.Tbl_Author.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Author_PreviousRefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Author')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Author_PreviousRefreshTokenHash
    ON dbo.Tbl_Author(PreviousRefreshTokenHash)
    WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Author_PreviousRefreshTokenHash on dbo.Tbl_Author.';
END;
GO

-- ============================================================================
-- 3. ENSURE DEFAULT SEED AUTHOR HAS VERIFIED BCRYPT WORK-FACTOR 12 HASH (Admin@123)
-- ============================================================================

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
    PRINT 'Inserted default seed author (elena.rostova@ledition.com / Admin@123).';
END
ELSE
BEGIN
    UPDATE dbo.Tbl_Author
    SET PasswordHash = N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.',
        IsApproved = 1,
        IsActive = 1
    WHERE AuthorId = '01JAUTHOR00000000000000001'
      AND PasswordHash = N'$2a$12$e0NRiA4b2VwYQcT7X8mOVuYqHhFhR8rUj2V0Wb8Z0XlK1mG5o6Pfa';
    PRINT 'Verified default seed author account (elena.rostova@ledition.com / Admin@123).';
END;
GO
