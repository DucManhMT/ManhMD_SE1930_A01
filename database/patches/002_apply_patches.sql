-- ============================================================================
-- Patch: 002_apply_patches.sql
-- Description: Applies schema fixes, trims data, fixes self-referencing parent seeds,
--              replaces CASCADE DELETE with NO ACTION, expands AccountPassword column,
--              and adds unique constraints/indexes.
-- Target DB: FUNewsManagement
-- ============================================================================

USE [FUNewsManagement];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

PRINT '--- APPLYING SCHEMA AND DATA PATCHES ---';

-- 1. Fix self-referencing parent category seed
UPDATE dbo.Category 
SET ParentCategoryID = NULL 
WHERE ParentCategoryID = CategoryID;
PRINT 'Updated self-referencing Category parent IDs to NULL.';

-- 2. Trim string columns to eliminate leading/trailing whitespace (e.g. "Alumni ")
UPDATE dbo.Tag 
SET TagName = LTRIM(RTRIM(TagName))
WHERE TagName IS NOT NULL AND TagName <> LTRIM(RTRIM(TagName));

UPDATE dbo.SystemAccount 
SET AccountEmail = LTRIM(RTRIM(AccountEmail))
WHERE AccountEmail IS NOT NULL AND AccountEmail <> LTRIM(RTRIM(AccountEmail));

UPDATE dbo.Category 
SET CategoryName = LTRIM(RTRIM(CategoryName))
WHERE CategoryName IS NOT NULL AND CategoryName <> LTRIM(RTRIM(CategoryName));
PRINT 'Trimmed whitespace from TagName, AccountEmail, CategoryName.';

-- 3. Expand AccountPassword column to nvarchar(512) to hold cryptographic password hashes
ALTER TABLE dbo.SystemAccount 
ALTER COLUMN AccountPassword nvarchar(512) NULL;
PRINT 'Expanded SystemAccount.AccountPassword to nvarchar(512).';

-- 4. Replace CASCADE DELETE on NewsArticle with NO ACTION to protect categories and accounts
IF OBJECT_ID('dbo.FK_NewsArticle_Category', 'F') IS NOT NULL
BEGIN
    ALTER TABLE dbo.NewsArticle DROP CONSTRAINT FK_NewsArticle_Category;
END
ALTER TABLE dbo.NewsArticle WITH CHECK ADD CONSTRAINT FK_NewsArticle_Category 
    FOREIGN KEY(CategoryID) REFERENCES dbo.Category(CategoryID)
    ON UPDATE CASCADE
    ON DELETE NO ACTION;
ALTER TABLE dbo.NewsArticle CHECK CONSTRAINT FK_NewsArticle_Category;
PRINT 'Replaced FK_NewsArticle_Category CASCADE DELETE with NO ACTION.';

IF OBJECT_ID('dbo.FK_NewsArticle_SystemAccount', 'F') IS NOT NULL
BEGIN
    ALTER TABLE dbo.NewsArticle DROP CONSTRAINT FK_NewsArticle_SystemAccount;
END
ALTER TABLE dbo.NewsArticle WITH CHECK ADD CONSTRAINT FK_NewsArticle_SystemAccount 
    FOREIGN KEY(CreatedByID) REFERENCES dbo.SystemAccount(AccountID)
    ON UPDATE CASCADE
    ON DELETE NO ACTION;
ALTER TABLE dbo.NewsArticle CHECK CONSTRAINT FK_NewsArticle_SystemAccount;
PRINT 'Replaced FK_NewsArticle_SystemAccount CASCADE DELETE with NO ACTION.';

-- 5. Create Unique Constraints / Filtered Indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_SystemAccount_AccountEmail' AND object_id = OBJECT_ID('dbo.SystemAccount'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_SystemAccount_AccountEmail 
    ON dbo.SystemAccount(AccountEmail) 
    WHERE AccountEmail IS NOT NULL;
    PRINT 'Created unique index UQ_SystemAccount_AccountEmail.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Tag_TagName' AND object_id = OBJECT_ID('dbo.Tag'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Tag_TagName 
    ON dbo.Tag(TagName) 
    WHERE TagName IS NOT NULL;
    PRINT 'Created unique index UQ_Tag_TagName.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Category_Name_ParentNotNull' AND object_id = OBJECT_ID('dbo.Category'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Category_Name_ParentNotNull 
    ON dbo.Category(ParentCategoryID, CategoryName) 
    WHERE ParentCategoryID IS NOT NULL;
    PRINT 'Created unique index UQ_Category_Name_ParentNotNull.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Category_Name_ParentNull' AND object_id = OBJECT_ID('dbo.Category'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Category_Name_ParentNull 
    ON dbo.Category(CategoryName) 
    WHERE ParentCategoryID IS NULL;
    PRINT 'Created unique index UQ_Category_Name_ParentNull.';
END

PRINT '--- SCHEMA AND DATA PATCHES APPLIED SUCCESSFULLY ---';
GO
