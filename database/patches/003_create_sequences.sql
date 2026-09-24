-- ============================================================================
-- Patch: 003_create_sequences.sql
-- Description: Creates SQL Sequences for non-identity primary keys (AccountID, TagID,
--              NewsArticleID) to ensure safe, concurrency-friendly ID generation.
-- Target DB: FUNewsManagement
-- ============================================================================

USE [FUNewsManagement];
GO

PRINT '--- CREATING SEQUENCES ---';

-- 1. Sequence for AccountID (smallint)
IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'Seq_AccountID' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DECLARE @NextAccount smallint;
    SELECT @NextAccount = CAST(ISNULL(MAX(AccountID), 0) + 1 AS smallint) FROM dbo.SystemAccount;
    IF @NextAccount < 1 SET @NextAccount = 1;

    DECLARE @SqlAcc nvarchar(256) = N'CREATE SEQUENCE dbo.Seq_AccountID AS smallint START WITH ' 
        + CAST(@NextAccount AS nvarchar(10)) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sp_executesql @SqlAcc;
    PRINT 'Created sequence dbo.Seq_AccountID starting at ' + CAST(@NextAccount AS nvarchar(10));
END
ELSE
BEGIN
    PRINT 'Sequence dbo.Seq_AccountID already exists.';
END
GO

-- 2. Sequence for TagID (int)
IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'Seq_TagID' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DECLARE @NextTag int;
    SELECT @NextTag = ISNULL(MAX(TagID), 0) + 1 FROM dbo.Tag;
    IF @NextTag < 1 SET @NextTag = 1;

    DECLARE @SqlTag nvarchar(256) = N'CREATE SEQUENCE dbo.Seq_TagID AS int START WITH ' 
        + CAST(@NextTag AS nvarchar(10)) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sp_executesql @SqlTag;
    PRINT 'Created sequence dbo.Seq_TagID starting at ' + CAST(@NextTag AS nvarchar(10));
END
ELSE
BEGIN
    PRINT 'Sequence dbo.Seq_TagID already exists.';
END
GO

-- 3. Sequence for NewsArticleID (bigint)
IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'Seq_NewsArticleID' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DECLARE @NextArticle bigint;
    SELECT @NextArticle = ISNULL(MAX(TRY_CAST(REPLACE(NewsArticleID, 'N', '') AS bigint)), 0) + 1 
    FROM dbo.NewsArticle;
    IF @NextArticle < 1 SET @NextArticle = 1;

    DECLARE @SqlArticle nvarchar(256) = N'CREATE SEQUENCE dbo.Seq_NewsArticleID AS bigint START WITH ' 
        + CAST(@NextArticle AS nvarchar(20)) + N' INCREMENT BY 1 NO CYCLE;';
    EXEC sp_executesql @SqlArticle;
    PRINT 'Created sequence dbo.Seq_NewsArticleID starting at ' + CAST(@NextArticle AS nvarchar(20));
END
ELSE
BEGIN
    PRINT 'Sequence dbo.Seq_NewsArticleID already exists.';
END
GO

PRINT '--- SEQUENCES CREATION COMPLETED ---';
GO
