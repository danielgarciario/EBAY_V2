USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[vwCurrentSellableInventory]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwCurrentSellableInventory];
END;
GO

IF OBJECT_ID(N'[ebay].[vwLatestInventoryImportBatch]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwLatestInventoryImportBatch];
END;
GO

IF OBJECT_ID(N'[ebay].[vwOpenArticleCompatibilityIssues]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwOpenArticleCompatibilityIssues];
END;
GO

CREATE VIEW [ebay].[vwLatestInventoryImportBatch]
AS
    SELECT
        [Id],
        [Source],
        [FeedType],
        [TaskId],
        [Status],
        [EbayAck],
        [SourceFileName],
        [SourceFileSha256],
        [StartedAtLocal],
        [CompletedAtLocal],
        [ImportedAtLocal],
        [RetentionUntilLocal],
        [SkuDetailsCount],
        [VariationCount]
    FROM [ebay].[InventoryImportBatch]
    WHERE [Id] =
    (
        SELECT TOP (1) [Id]
        FROM [ebay].[InventoryImportBatch]
        WHERE [Status] = N'Imported'
          AND ([EbayAck] IS NULL OR [EbayAck] = N'Success')
        ORDER BY [ImportedAtLocal] DESC, [Id] DESC
    );
GO

CREATE VIEW [ebay].[vwCurrentSellableInventory]
AS
    SELECT
        s.[Id] AS [SellableSnapshotId],
        s.[ImportBatchId],
        b.[ImportedAtLocal],
        s.[ListingSnapshotId],
        s.[EbayItemId],
        s.[ParentSku],
        s.[SellableSku],
        s.[IsVariation],
        s.[ReportedQuantity],
        s.[PriceAmount],
        s.[PriceCurrency],
        v.[ParsedEfaItem],
        v.[ParsedEfaSalesUnit],
        v.[ValidationStatus],
        v.[EfaItemFound],
        v.[EfaSalesUnitFound],
        v.[EfaItemIsActive],
        v.[ValidatedAtLocal]
    FROM [ebay].[InventorySellableSnapshot] AS s
    INNER JOIN [ebay].[vwLatestInventoryImportBatch] AS b
        ON b.[Id] = s.[ImportBatchId]
    LEFT JOIN [ebay].[InventorySellableEfaValidation] AS v
        ON v.[SellableSnapshotId] = s.[Id];
GO

CREATE VIEW [ebay].[vwOpenArticleCompatibilityIssues]
AS
    SELECT
        i.[Id],
        i.[ImportBatchId],
        b.[ImportedAtLocal],
        i.[EbayItemId],
        i.[SellableSku],
        i.[ParsedEfaItem],
        i.[ParsedEfaSalesUnit],
        i.[IssueType],
        i.[Severity],
        i.[Message],
        i.[DetectedAtLocal]
    FROM [ebay].[ArticleCompatibilityIssue] AS i
    INNER JOIN [ebay].[InventoryImportBatch] AS b
        ON b.[Id] = i.[ImportBatchId]
    WHERE i.[ResolvedAtLocal] IS NULL;
GO
