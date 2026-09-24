-- ============================================================================
-- Patch: 001_preflight_check.sql
-- Description: Preflight inspection to detect duplicate keys, invalid self-referencing
--              parent categories, or constraints before applying schema patches.
-- Target DB: FUNewsManagement
-- ============================================================================

USE [FUNewsManagement];
GO

PRINT '--- PREFLIGHT CHECK START ---';

-- 1. Check duplicate emails in SystemAccount
SELECT 
    LOWER(LTRIM(RTRIM(AccountEmail))) AS NormalizedEmail, 
    COUNT(*) AS DuplicateCount
FROM dbo.SystemAccount
WHERE AccountEmail IS NOT NULL
GROUP BY LOWER(LTRIM(RTRIM(AccountEmail)))
HAVING COUNT(*) > 1;

-- 2. Check duplicate Tag names (after trimming)
SELECT 
    LOWER(LTRIM(RTRIM(TagName))) AS NormalizedTagName, 
    COUNT(*) AS DuplicateCount
FROM dbo.Tag
WHERE TagName IS NOT NULL
GROUP BY LOWER(LTRIM(RTRIM(TagName)))
HAVING COUNT(*) > 1;

-- 3. Check duplicate Category names with same parent (including NULL parent)
SELECT 
    ParentCategoryID, 
    LOWER(LTRIM(RTRIM(CategoryName))) AS NormalizedCategoryName, 
    COUNT(*) AS DuplicateCount
FROM dbo.Category
GROUP BY ParentCategoryID, LOWER(LTRIM(RTRIM(CategoryName)))
HAVING COUNT(*) > 1;

-- 4. Check Category rows where ParentCategoryID = CategoryID (self-referencing seed bug)
SELECT 
    CategoryID, 
    CategoryName, 
    ParentCategoryID
FROM dbo.Category
WHERE ParentCategoryID = CategoryID;

-- 5. Check foreign key delete actions on NewsArticle
SELECT 
    fk.name AS ConstraintName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('dbo.NewsArticle');

PRINT '--- PREFLIGHT CHECK COMPLETED ---';
GO
