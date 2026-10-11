-- ============================================================================
-- MIGRATION 05: ADMIN AUTHENTICATION, ROTATING REFRESH TOKEN & 30s GRACE PERIOD
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- Safe to run multiple times (Idempotent)
-- ============================================================================

USE MagizineDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. ADD REFRESH TOKEN & 30-SECOND GRACE PERIOD COLUMNS TO dbo.Tbl_Admin
-- ============================================================================

IF COL_LENGTH('dbo.Tbl_Admin', 'RefreshTokenHash') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Admin ADD RefreshTokenHash NVARCHAR(128) NULL;
    PRINT 'Added column RefreshTokenHash to dbo.Tbl_Admin.';
END;

IF COL_LENGTH('dbo.Tbl_Admin', 'RefreshTokenExpiresAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Admin ADD RefreshTokenExpiresAt DATETIMEOFFSET NULL;
    PRINT 'Added column RefreshTokenExpiresAt to dbo.Tbl_Admin.';
END;

IF COL_LENGTH('dbo.Tbl_Admin', 'PreviousRefreshTokenHash') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Admin ADD PreviousRefreshTokenHash NVARCHAR(128) NULL;
    PRINT 'Added column PreviousRefreshTokenHash to dbo.Tbl_Admin.';
END;

IF COL_LENGTH('dbo.Tbl_Admin', 'PreviousRefreshTokenExpiresAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tbl_Admin ADD PreviousRefreshTokenExpiresAt DATETIMEOFFSET NULL;
    PRINT 'Added column PreviousRefreshTokenExpiresAt to dbo.Tbl_Admin.';
END;
GO

-- ============================================================================
-- 2. CREATE FILTERED INDEXES FOR FAST O(1) REFRESH TOKEN LOOKUP
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Admin_RefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Admin')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Admin_RefreshTokenHash
    ON dbo.Tbl_Admin(RefreshTokenHash)
    WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Admin_RefreshTokenHash on dbo.Tbl_Admin.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Admin_PreviousRefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Admin')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Admin_PreviousRefreshTokenHash
    ON dbo.Tbl_Admin(PreviousRefreshTokenHash)
    WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Admin_PreviousRefreshTokenHash on dbo.Tbl_Admin.';
END;
GO

-- ============================================================================
-- 3. ENSURE DEFAULT SUPERADMIN HAS VERIFIED BCRYPT WORK-FACTOR 12 HASH (Admin@123)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Admin WHERE Username = 'superadmin')
BEGIN
    INSERT INTO dbo.Tbl_Admin (AdminId, Username, Email, PasswordHash, Role, IsActive, CreatedAt)
    VALUES (
        '01JADMIN000000000000000001',
        N'superadmin',
        N'admin@ledition.com',
        N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.',
        N'SuperAdmin',
        1,
        SYSDATETIMEOFFSET()
    );
    PRINT 'Inserted default superadmin account (Password: Admin@123).';
END
ELSE
BEGIN
    -- Update placeholder hash if still set to the initial non-BCrypt placeholder
    UPDATE dbo.Tbl_Admin
    SET PasswordHash = N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.'
    WHERE (AdminId = '01JADMIN000000000000000001' OR Username = N'superadmin')
      AND PasswordHash = N'$2a$12$e0NRiA4b2VwYQcT7X8mOVuYqHhFhR8rUj2V0Wb8Z0XlK1mG5o6Pfa';
    PRINT 'Verified/Updated superadmin BCrypt password hash (Password: Admin@123).';
END;

-- Also update Default Seed Author if still using the placeholder hash
UPDATE dbo.Tbl_Author
SET PasswordHash = N'$2a$12$TIcVIJCHDmVR.1v63Ap/4eFbqiviwmsBe6Di5kkDaiuiD58n5HxN.'
WHERE AuthorId = '01JAUTHOR00000000000000001'
  AND PasswordHash = N'$2a$12$e0NRiA4b2VwYQcT7X8mOVuYqHhFhR8rUj2V0Wb8Z0XlK1mG5o6Pfa';
GO
