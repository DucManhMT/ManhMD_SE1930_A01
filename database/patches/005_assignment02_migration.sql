-- ==============================================================================
-- PRN232 ASSIGNMENT 02 - DATABASE MIGRATION PATCH
-- Tương thích ngược hoàn toàn với Database Assignment 01
-- ==============================================================================

USE [FUNewsManagement];
GO

-- 1. Bổ sung cột ảnh bài viết
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('NewsArticle') AND name = 'NewsImage')
BEGIN
    ALTER TABLE NewsArticle ADD NewsImage NVARCHAR(500) NULL;
    PRINT 'Added NewsImage column to NewsArticle table.';
END
GO

-- 2. Tạo bảng AuditLog
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        AuditID BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NULL,
        UserName NVARCHAR(100) NULL,
        Action NVARCHAR(50) NOT NULL,
        Entity NVARCHAR(100) NOT NULL,
        EntityID NVARCHAR(50) NULL,
        BeforeJSON NVARCHAR(MAX) NULL,
        AfterJSON NVARCHAR(MAX) NULL,
        Timestamp DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_AuditLog_Timestamp ON AuditLog(Timestamp DESC);
    PRINT 'Created AuditLog table.';
END
GO

-- 3. Tạo bảng RefreshToken (hỗ trợ cả AccountID trong DB và Admin config qua UserEmail)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshToken')
BEGIN
    CREATE TABLE RefreshToken (
        TokenID BIGINT IDENTITY(1,1) PRIMARY KEY,
        AccountID SMALLINT NULL,
        UserEmail NVARCHAR(100) NULL,
        Token NVARCHAR(512) NOT NULL UNIQUE,
        ExpiresAt DATETIME NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        IsRevoked BIT NOT NULL DEFAULT 0,
        ReplacedByToken NVARCHAR(512) NULL,
        CONSTRAINT FK_RefreshToken_Account FOREIGN KEY (AccountID) 
            REFERENCES SystemAccount(AccountID) ON DELETE CASCADE
    );
    CREATE INDEX IX_RefreshToken_AccountID ON RefreshToken(AccountID);
    CREATE INDEX IX_RefreshToken_Token ON RefreshToken(Token);
    PRINT 'Created RefreshToken table.';
END
GO

-- 4. Tạo bảng NewsView
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NewsView')
BEGIN
    CREATE TABLE NewsView (
        ViewID BIGINT IDENTITY(1,1) PRIMARY KEY,
        NewsArticleID NVARCHAR(20) NOT NULL,
        ViewedAt DATETIME NOT NULL DEFAULT GETDATE(),
        VisitorIP NVARCHAR(50) NULL,
        CONSTRAINT FK_NewsView_NewsArticle FOREIGN KEY (NewsArticleID) 
            REFERENCES NewsArticle(NewsArticleID) ON DELETE CASCADE
    );
    CREATE INDEX IX_NewsView_Article_Date ON NewsView(NewsArticleID, ViewedAt);
    PRINT 'Created NewsView table.';
END
GO

-- 5. Tạo bảng TagLearning
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TagLearning')
BEGIN
    CREATE TABLE TagLearning (
        TagID INT PRIMARY KEY,
        SelectionCount INT NOT NULL DEFAULT 0,
        LastSelectedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_TagLearning_Tag FOREIGN KEY (TagID) 
            REFERENCES Tag(TagID) ON DELETE CASCADE
    );
    PRINT 'Created TagLearning table.';
END
GO

PRINT 'Assignment 02 Database Migration applied successfully!';
GO
