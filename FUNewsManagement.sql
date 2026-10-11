-- ==============================================================================
-- PRN232 ASSIGNMENT 02 - CONSOLIDATED DATABASE SCRIPT
-- Database: FUNewsManagement
-- Author: ManhMD (SE1930)
-- Description: Complete schema, constraints, indexes, A02 extensions, and seed data.
-- ==============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FUNewsManagement')
BEGIN
    CREATE DATABASE [FUNewsManagement];
    PRINT 'Database [FUNewsManagement] created successfully.';
END
GO

USE [FUNewsManagement];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ==============================================================================
-- 1. CLEANUP OLD CONSTRAINTS AND TABLES (IF RE-CREATING)
-- ==============================================================================
IF OBJECT_ID(N'dbo.NewsTag', 'U') IS NOT NULL DROP TABLE dbo.NewsTag;
IF OBJECT_ID(N'dbo.NewsView', 'U') IS NOT NULL DROP TABLE dbo.NewsView;
IF OBJECT_ID(N'dbo.TagLearning', 'U') IS NOT NULL DROP TABLE dbo.TagLearning;
IF OBJECT_ID(N'dbo.NewsArticle', 'U') IS NOT NULL DROP TABLE dbo.NewsArticle;
IF OBJECT_ID(N'dbo.RefreshToken', 'U') IS NOT NULL DROP TABLE dbo.RefreshToken;
IF OBJECT_ID(N'dbo.AuditLog', 'U') IS NOT NULL DROP TABLE dbo.AuditLog;
IF OBJECT_ID(N'dbo.Tag', 'U') IS NOT NULL DROP TABLE dbo.Tag;
IF OBJECT_ID(N'dbo.Category', 'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID(N'dbo.SystemAccount', 'U') IS NOT NULL DROP TABLE dbo.SystemAccount;
GO

-- ==============================================================================
-- 2. CREATE CORE & EXTENSION TABLES
-- ==============================================================================

-- 2.1 Table: SystemAccount
CREATE TABLE dbo.SystemAccount (
    AccountID SMALLINT NOT NULL,
    AccountName NVARCHAR(100) NULL,
    AccountEmail NVARCHAR(70) NULL,
    AccountRole INT NULL, -- 1: Staff, 2: Lecturer, 0: Admin (nếu lưu trong DB)
    AccountPassword NVARCHAR(512) NULL,
    CONSTRAINT PK_SystemAccount PRIMARY KEY CLUSTERED (AccountID ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX UQ_SystemAccount_AccountEmail 
ON dbo.SystemAccount(AccountEmail) 
WHERE AccountEmail IS NOT NULL;
GO

-- 2.2 Table: Category
CREATE TABLE dbo.Category (
    CategoryID INT IDENTITY(1,1) NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    CategoryDesciption NVARCHAR(250) NOT NULL,
    ParentCategoryID INT NULL,
    IsActive BIT NULL DEFAULT 1,
    CONSTRAINT PK_Category PRIMARY KEY CLUSTERED (CategoryID ASC),
    CONSTRAINT FK_Category_Category FOREIGN KEY (ParentCategoryID) 
        REFERENCES dbo.Category (CategoryID)
);
CREATE UNIQUE NONCLUSTERED INDEX UQ_Category_Name_ParentNotNull 
ON dbo.Category(ParentCategoryID, CategoryName) 
WHERE ParentCategoryID IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UQ_Category_Name_ParentNull 
ON dbo.Category(CategoryName) 
WHERE ParentCategoryID IS NULL;
GO

-- 2.3 Table: Tag
CREATE TABLE dbo.Tag (
    TagID INT NOT NULL,
    TagName NVARCHAR(50) NULL,
    Note NVARCHAR(400) NULL,
    CONSTRAINT PK_HashTag PRIMARY KEY CLUSTERED (TagID ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX UQ_Tag_TagName 
ON dbo.Tag(TagName) 
WHERE TagName IS NOT NULL;
GO

-- 2.4 Table: NewsArticle
CREATE TABLE dbo.NewsArticle (
    NewsArticleID NVARCHAR(20) NOT NULL,
    NewsTitle NVARCHAR(400) NULL,
    Headline NVARCHAR(150) NOT NULL,
    CreatedDate DATETIME NULL DEFAULT GETDATE(),
    NewsContent NVARCHAR(4000) NULL,
    NewsSource NVARCHAR(400) NULL,
    CategoryID INT NULL,
    NewsStatus BIT NULL DEFAULT 1,
    CreatedByID SMALLINT NULL,
    UpdatedByID SMALLINT NULL,
    ModifiedDate DATETIME NULL,
    NewsImage NVARCHAR(500) NULL,
    CONSTRAINT PK_NewsArticle PRIMARY KEY CLUSTERED (NewsArticleID ASC),
    CONSTRAINT FK_NewsArticle_Category FOREIGN KEY (CategoryID) 
        REFERENCES dbo.Category (CategoryID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,
    CONSTRAINT FK_NewsArticle_SystemAccount FOREIGN KEY (CreatedByID) 
        REFERENCES dbo.SystemAccount (AccountID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,
    CONSTRAINT FK_NewsArticle_SystemAccount_Update FOREIGN KEY (UpdatedByID) 
        REFERENCES dbo.SystemAccount (AccountID)
        ON DELETE NO ACTION
);
CREATE NONCLUSTERED INDEX IX_NewsArticle_CreatedDate ON dbo.NewsArticle(CreatedDate DESC);
CREATE NONCLUSTERED INDEX IX_NewsArticle_Category ON dbo.NewsArticle(CategoryID);
GO

-- 2.5 Table: NewsTag
CREATE TABLE dbo.NewsTag (
    NewsArticleID NVARCHAR(20) NOT NULL,
    TagID INT NOT NULL,
    CONSTRAINT PK_NewsTag PRIMARY KEY CLUSTERED (NewsArticleID ASC, TagID ASC),
    CONSTRAINT FK_NewsTag_NewsArticle FOREIGN KEY (NewsArticleID) 
        REFERENCES dbo.NewsArticle (NewsArticleID) 
        ON DELETE CASCADE,
    CONSTRAINT FK_NewsTag_Tag FOREIGN KEY (TagID) 
        REFERENCES dbo.Tag (TagID)
);
GO

-- 2.6 Table: RefreshToken (Assignment 02)
CREATE TABLE dbo.RefreshToken (
    TokenID BIGINT IDENTITY(1,1) NOT NULL,
    AccountID SMALLINT NULL,
    UserEmail NVARCHAR(100) NULL,
    Token NVARCHAR(512) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    IsRevoked BIT NOT NULL DEFAULT 0,
    ReplacedByToken NVARCHAR(512) NULL,
    CONSTRAINT PK_RefreshToken PRIMARY KEY CLUSTERED (TokenID ASC),
    CONSTRAINT UQ_RefreshToken_Token UNIQUE (Token),
    CONSTRAINT FK_RefreshToken_Account FOREIGN KEY (AccountID) 
        REFERENCES dbo.SystemAccount(AccountID) 
        ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_RefreshToken_AccountID ON dbo.RefreshToken(AccountID);
GO

-- 2.7 Table: AuditLog (Assignment 02)
CREATE TABLE dbo.AuditLog (
    AuditID BIGINT IDENTITY(1,1) NOT NULL,
    UserID INT NULL,
    UserName NVARCHAR(100) NULL,
    Action NVARCHAR(50) NOT NULL,
    Entity NVARCHAR(100) NOT NULL,
    EntityID NVARCHAR(50) NULL,
    BeforeJSON NVARCHAR(MAX) NULL,
    AfterJSON NVARCHAR(MAX) NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_AuditLog PRIMARY KEY CLUSTERED (AuditID ASC)
);
CREATE NONCLUSTERED INDEX IX_AuditLog_Timestamp ON dbo.AuditLog(Timestamp DESC);
GO

-- 2.8 Table: NewsView (Assignment 02 - Analytics Trending)
CREATE TABLE dbo.NewsView (
    ViewID BIGINT IDENTITY(1,1) NOT NULL,
    NewsArticleID NVARCHAR(20) NOT NULL,
    ViewedAt DATETIME NOT NULL DEFAULT GETDATE(),
    VisitorIP NVARCHAR(50) NULL,
    CONSTRAINT PK_NewsView PRIMARY KEY CLUSTERED (ViewID ASC),
    CONSTRAINT FK_NewsView_NewsArticle FOREIGN KEY (NewsArticleID) 
        REFERENCES dbo.NewsArticle(NewsArticleID) 
        ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX IX_NewsView_Article_Date ON dbo.NewsView(NewsArticleID, ViewedAt);
GO

-- 2.9 Table: TagLearning (Assignment 02 - AI Engine Cache)
CREATE TABLE dbo.TagLearning (
    TagID INT NOT NULL,
    SelectionCount INT NOT NULL DEFAULT 0,
    LastSelectedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_TagLearning PRIMARY KEY CLUSTERED (TagID ASC),
    CONSTRAINT FK_TagLearning_Tag FOREIGN KEY (TagID) 
        REFERENCES dbo.Tag(TagID) 
        ON DELETE CASCADE
);
GO

-- ==============================================================================
-- 3. SEED INITIAL DATA
-- ==============================================================================

-- 3.1 Accounts (Staff & Lecturer với mật khẩu '@1')
INSERT INTO dbo.SystemAccount (AccountID, AccountName, AccountEmail, AccountRole, AccountPassword) VALUES
(1, N'Alexander David', N'Alexander@FUNewsManagement.org', 2, N'@1'),
(2, N'Emma Watson', N'EmmaWatson@FUNewsManagement.org', 2, N'@1'),
(3, N'Isabella David', N'IsabellaDavid@FUNewsManagement.org', 1, N'@1'),
(4, N'Liam Johnson', N'LiamJohnson@FUNewsManagement.org', 1, N'@1'),
(5, N'Sophia Williams', N'SophiaWilliams@FUNewsManagement.org', 1, N'@1');
GO

-- 3.2 Categories
SET IDENTITY_INSERT dbo.Category ON;
INSERT INTO dbo.Category (CategoryID, CategoryName, CategoryDesciption, ParentCategoryID, IsActive) VALUES
(1, N'Công nghệ & AI', N'Tin tức về công nghệ thông tin, trí tuệ nhân tạo và chuyển đổi số', NULL, 1),
(2, N'Đời sống học đường', N'Hoạt động sinh viên, phong trào thanh niên và câu lạc bộ', NULL, 1),
(3, N'Nghiên cứu & Học thuật', N'Các công trình nghiên cứu khoa học và hội thảo học thuật', NULL, 1),
(4, N'Tuyển dụng & Việc làm', N'Thông tin thực tập, tuyển dụng và cơ hội việc làm doanh nghiệp', NULL, 1),
(5, N'Trí tuệ nhân tạo GenAI', N'Chuyên mục con về các ứng dụng Generative AI và LLM', 1, 1),
(6, N'Thể thao FPT', N'Hoạt động thể dục thể thao và giải đấu bóng đá', 2, 1);
SET IDENTITY_INSERT dbo.Category OFF;
GO

-- 3.3 Tags
INSERT INTO dbo.Tag (TagID, TagName, Note) VALUES
(1, N'AI', N'Trí tuệ nhân tạo và học máy'),
(2, N'DotNet', N'Nền tảng phát triển ứng dụng .NET 8'),
(3, N'SinhVien', N'Đời sống và hoạt động sinh viên'),
(4, N'TuyenDung', N'Cơ hội việc làm và tuyển dụng IT'),
(5, N'NghienCuu', N'Công trình nghiên cứu khoa học'),
(6, N'FPT', N'Tập đoàn FPT và Đại học FPT'),
(7, N'Seminar', N'Hội thảo chuyên đề công nghệ');
GO

-- 3.4 News Articles
INSERT INTO dbo.NewsArticle (NewsArticleID, NewsTitle, Headline, CreatedDate, NewsContent, NewsSource, CategoryID, NewsStatus, CreatedByID, UpdatedByID, ModifiedDate, NewsImage) VALUES
(N'N001', N'FPT University ứng dụng Trí tuệ nhân tạo vào giảng dạy 2026', N'Bước đột phá mới trong giáo dục đại học thời kỳ chuyển đổi số', DATEADD(DAY, -5, GETDATE()), N'Nội dung chi tiết về việc ứng dụng AI và các trợ lý thông minh hỗ trợ sinh viên học lập trình, phân tích dữ liệu và nghiên cứu khoa học...', N'Ban Truyền thông FPT', 1, 1, 3, 3, GETDATE(), N'/uploads/news/ai-education.jpg'),
(N'N002', N'Hội thảo chuyên đề: .NET 8 và kiến trúc Microservices phân tán', N'Diễn giả chuyên gia trình bày giải pháp tối ưu hệ thống doanh nghiệp', DATEADD(DAY, -4, GETDATE()), N'Hội thảo quy tụ hơn 500 sinh viên ngành Kỹ thuật phần mềm cùng các chuyên gia hàng đầu thảo luận về Clean Architecture, N-Tier và SignalR...', N'CLB Lập trình FPT', 1, 1, 3, NULL, NULL, N'/uploads/news/dotnet8-conf.jpg'),
(N'N003', N'Ngày hội việc làm FPT Career Fair 2026 mở ra 1000 cơ hội', N'Hơn 50 doanh nghiệp công nghệ hàng đầu tham gia tuyển dụng trực tiếp', DATEADD(DAY, -3, GETDATE()), N'Các cơ hội thực tập và việc làm hấp dẫn dành cho sinh viên IT từ các đối tác lớn FPT Software, Viettel, VNPT, Momo...', N'Phòng Quan hệ Doanh nghiệp', 4, 1, 4, NULL, NULL, NULL),
(N'N004', N'Giải bóng đá sinh viên FU Cup 2026 chính thức khởi tranh', N'32 đội bóng tranh tài sôi nổi tại sân vận động FPT Hòa Lạc', DATEADD(DAY, -2, GETDATE()), N'Lễ khai mạc diễn ra hoành tráng với sự tham gia của đông đảo cán bộ, giảng viên và cổ động viên sinh viên...', N'Phòng Công tác Sinh viên', 6, 1, 4, NULL, NULL, NULL),
(N'N005', N'Nhóm nghiên cứu FPT công bố bài báo khoa học chuẩn Q1 quốc tế', N'Đóng góp quan trọng vào lĩnh vực xử lý ngôn ngữ tự nhiên tiếng Việt', DATEADD(DAY, -1, GETDATE()), N'Nghiên cứu tập trung vào việc fine-tuning mô hình ngôn ngữ lớn cho tài liệu học thuật tiếng Việt với độ chính xác cao...', N'Viện Nghiên cứu FPT', 3, 1, 3, NULL, NULL, NULL);
GO

-- 3.5 NewsTag Mappings
INSERT INTO dbo.NewsTag (NewsArticleID, TagID) VALUES
(N'N001', 1),
(N'N001', 6),
(N'N002', 1),
(N'N002', 2),
(N'N002', 7),
(N'N003', 4),
(N'N003', 6),
(N'N004', 3),
(N'N004', 6),
(N'N005', 1),
(N'N005', 5);
GO

-- 3.6 Sample News Views (cho Analytics Trending)
INSERT INTO dbo.NewsView (NewsArticleID, ViewedAt, VisitorIP) VALUES
(N'N001', DATEADD(HOUR, -2, GETDATE()), N'127.0.0.1'),
(N'N001', DATEADD(HOUR, -1, GETDATE()), N'127.0.0.1'),
(N'N001', GETDATE(), N'127.0.0.1'),
(N'N002', DATEADD(HOUR, -3, GETDATE()), N'127.0.0.1'),
(N'N002', GETDATE(), N'127.0.0.1'),
(N'N005', GETDATE(), N'127.0.0.1');
GO

-- 3.7 Sample Tag Learning
INSERT INTO dbo.TagLearning (TagID, SelectionCount, LastSelectedAt) VALUES
(1, 15, GETDATE()),
(2, 8, GETDATE()),
(6, 12, GETDATE());
GO

PRINT '==============================================================================';
PRINT 'CONSOLIDATED DATABASE [FUNewsManagement] INITIALIZED SUCCESSFULLY!';
PRINT 'Sample accounts:';
PRINT '  Staff:    IsabellaDavid@FUNewsManagement.org / @1';
PRINT '  Lecturer: Alexander@FUNewsManagement.org     / @1';
PRINT '  Admin:    admin@FUNewsManagementSystem.org   / @@abc123@@ (từ config)';
PRINT '==============================================================================';
GO
