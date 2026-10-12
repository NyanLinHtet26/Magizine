-- ============================================================================
-- MIGRATION 07: PUBLIC READER AUTHENTICATION, ROTATING REFRESH TOKEN & 30s GRACE PERIOD
-- Target Engine: Microsoft SQL Server 2017+ / Azure SQL Database
-- Safe to run multiple times (Idempotent)
-- ============================================================================

USE MagizineDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================================
-- 1. CREATE TABLE dbo.Tbl_Reader IF NOT EXISTS
-- ============================================================================

IF OBJECT_ID('dbo.Tbl_Reader', 'U') IS NULL
BEGIN
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
        IsActive                      BIT               NOT NULL CONSTRAINT DF_Reader_IsActive DEFAULT (1),
        CreatedAt                     DATETIMEOFFSET    NOT NULL CONSTRAINT DF_Reader_CreatedAt DEFAULT (SYSDATETIMEOFFSET()),
        UpdatedAt                     DATETIMEOFFSET    NULL,
        IsDeleted                     BIT               NOT NULL CONSTRAINT DF_Reader_IsDeleted DEFAULT (0),
        DeletedAt                     DATETIMEOFFSET    NULL,
        DeletedByAdminId              VARCHAR(26)       NULL CONSTRAINT FK_Reader_DeletedByAdmin REFERENCES dbo.Tbl_Admin(AdminId)
    );
    PRINT 'Created table dbo.Tbl_Reader.';
END;
GO

-- ============================================================================
-- 2. CREATE FILTERED UNIQUE & REFRESH TOKEN INDEXES ON dbo.Tbl_Reader
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Tbl_Reader_Username_NotDeleted'
      AND object_id = OBJECT_ID('dbo.Tbl_Reader')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Reader_Username_NotDeleted
    ON dbo.Tbl_Reader(Username)
    WHERE IsDeleted = 0;
    PRINT 'Created index UX_Tbl_Reader_Username_NotDeleted on dbo.Tbl_Reader.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Tbl_Reader_Email_NotDeleted'
      AND object_id = OBJECT_ID('dbo.Tbl_Reader')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tbl_Reader_Email_NotDeleted
    ON dbo.Tbl_Reader(Email)
    WHERE IsDeleted = 0;
    PRINT 'Created index UX_Tbl_Reader_Email_NotDeleted on dbo.Tbl_Reader.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Reader_RefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Reader')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Reader_RefreshTokenHash
    ON dbo.Tbl_Reader(RefreshTokenHash)
    WHERE RefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Reader_RefreshTokenHash on dbo.Tbl_Reader.';
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Tbl_Reader_PreviousRefreshTokenHash'
      AND object_id = OBJECT_ID('dbo.Tbl_Reader')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tbl_Reader_PreviousRefreshTokenHash
    ON dbo.Tbl_Reader(PreviousRefreshTokenHash)
    WHERE PreviousRefreshTokenHash IS NOT NULL AND IsDeleted = 0;
    PRINT 'Created index IX_Tbl_Reader_PreviousRefreshTokenHash on dbo.Tbl_Reader.';
END;
GO

-- ============================================================================
-- 3. SEED DEFAULT READER ACCOUNT (sophia.reader / Admin@123)
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Reader WHERE ReaderId = '01JREADER00000000000000001')
BEGIN
    INSERT INTO dbo.Tbl_Reader (
        ReaderId, FirstName, LastName, Username, Email, PasswordHash,
        AvatarUrl, Bio, IsActive, CreatedAt
    )
    VALUES (
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
    PRINT 'Inserted default seed reader (sophia.reader / Admin@123).';
END;
GO
