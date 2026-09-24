-- ============================================================================
-- Patch: 004_verify_patches.sql
-- Description: Verification script to check that constraints, sequences,
--              column lengths, and data integrity match requirements.
-- Target DB: FUNewsManagement
-- ============================================================================

USE [FUNewsManagement];
GO

PRINT '--- VERIFYING PATCHES AND INTEGRITY ---';

-- 1. Verify foreign key delete actions on NewsArticle (Must be NO ACTION)
SELECT 
    fk.name AS ConstraintName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('dbo.NewsArticle');

-- 2. Verify self-referencing rows in Category (Must be 0 rows)
SELECT 
    CategoryID, 
    CategoryName, 
    ParentCategoryID
FROM dbo.Category
WHERE ParentCategoryID = CategoryID;

-- 3. Verify unique indexes
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.is_unique AS IsUnique,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name IN (
    'UQ_SystemAccount_AccountEmail',
    'UQ_Tag_TagName',
    'UQ_Category_Name_ParentNotNull',
    'UQ_Category_Name_ParentNull'
);

-- 4. Verify sequences
SELECT 
    name AS SequenceName,
    start_value AS StartValue,
    current_value AS CurrentValue,
    minimum_value AS MinValue,
    maximum_value AS MaxValue,
    increment AS IncrementValue
FROM sys.sequences
WHERE name IN ('Seq_AccountID', 'Seq_TagID', 'Seq_NewsArticleID');

-- 5. Verify AccountPassword column length (Must be 512)
SELECT 
    TABLE_NAME, 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'SystemAccount' AND COLUMN_NAME = 'AccountPassword';

PRINT '--- VERIFICATION COMPLETED ---';
GO
