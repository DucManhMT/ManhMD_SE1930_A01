# Bản Vá & Mở Rộng Cơ Sở Dữ Liệu (Database Migration) — PRN232 A02

Tài liệu thiết kế lược đồ CSDL bổ sung (Add-on Schema) cho Assignment 02 trên nền tảng Database `FUNewsManagement` đã có từ Assignment 01. Toàn bộ các bảng và cột mới được thiết kế **không làm phá vỡ dữ liệu cũ** và hoàn toàn tương thích ngược.

---

## 1. Thiết Kế Các Bảng Mới (New Tables)

### 1.1 Bảng `AuditLog` (Nhật ký thay đổi dữ liệu)
Phục vụ yêu cầu: *"Audit Logging to record user actions (User, Action, Entity, Before/After JSON)"*.

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        AuditID BIGINT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NULL,                         -- AccountID của người thực hiện thao tác
        UserName NVARCHAR(100) NULL,             -- Tên hoặc Email người thực hiện
        Action NVARCHAR(50) NOT NULL,            -- CREATE, UPDATE, DELETE, LOGIN, CHANGE_PASSWORD
        Entity NVARCHAR(100) NOT NULL,           -- NewsArticle, Category, Tag, SystemAccount
        EntityID NVARCHAR(50) NULL,              -- Khóa chính của đối tượng bị thay đổi
        BeforeJSON NVARCHAR(MAX) NULL,           -- Dữ liệu trước khi sửa (JSON)
        AfterJSON NVARCHAR(MAX) NULL,            -- Dữ liệu sau khi sửa (JSON)
        Timestamp DATETIME NOT NULL DEFAULT GETDATE()
    );

    CREATE INDEX IX_AuditLog_Timestamp ON AuditLog(Timestamp DESC);
    CREATE INDEX IX_AuditLog_Entity ON AuditLog(Entity, EntityID);
    CREATE INDEX IX_AuditLog_UserID ON AuditLog(UserID);
END
```

### 1.2 Bảng `RefreshToken` (Quản lý cấp và thu hồi JWT Refresh Token)
Phục vụ yêu cầu: *"JWT Authentication and issue Refresh Tokens via /api/auth/refresh"*.

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshToken')
BEGIN
    CREATE TABLE RefreshToken (
        TokenID BIGINT IDENTITY(1,1) PRIMARY KEY,
        AccountID SMALLINT NOT NULL,
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
END
```

### 1.3 Bảng `NewsView` (Theo dõi lượt đọc để tính Trending)
Phục vụ yêu cầu: *"List trending (most viewed) articles"*.

```sql
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
END
```

### 1.4 Bảng `TagLearning` (Bộ nhớ học tần suất chọn thẻ tin của AI)
Phục vụ yêu cầu: *"Maintain a Learning Cache to remember tags frequently selected by users"*.

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TagLearning')
BEGIN
    CREATE TABLE TagLearning (
        TagID INT PRIMARY KEY,
        SelectionCount INT NOT NULL DEFAULT 0,
        LastSelectedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_TagLearning_Tag FOREIGN KEY (TagID) 
            REFERENCES Tag(TagID) ON DELETE CASCADE
    );
END
```

---

## 2. Bổ Sung Cột Vào Bảng Hiện Có (Table Alterations)

### Bổ sung cột lưu đường dẫn ảnh cho `NewsArticle`
Phục vụ yêu cầu: *"Allow image uploads for articles, validating file type and size"*.

```sql
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('NewsArticle') AND name = 'NewsImage'
)
BEGIN
    ALTER TABLE NewsArticle ADD NewsImage NVARCHAR(500) NULL;
END
```

---

## 3. Kịch Bản Chạy Di Trú Hoàn Chỉnh (Complete Add-on Script)

File script tổng hợp được đặt tại:
`database/patches/005_assignment02_migration.sql`

```sql
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

-- 3. Tạo bảng RefreshToken
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshToken')
BEGIN
    CREATE TABLE RefreshToken (
        TokenID BIGINT IDENTITY(1,1) PRIMARY KEY,
        AccountID SMALLINT NOT NULL,
        Token NVARCHAR(512) NOT NULL UNIQUE,
        ExpiresAt DATETIME NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        IsRevoked BIT NOT NULL DEFAULT 0,
        ReplacedByToken NVARCHAR(512) NULL,
        CONSTRAINT FK_RefreshToken_Account FOREIGN KEY (AccountID) 
            REFERENCES SystemAccount(AccountID) ON DELETE CASCADE
    );
    CREATE INDEX IX_RefreshToken_AccountID ON RefreshToken(AccountID);
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
```
