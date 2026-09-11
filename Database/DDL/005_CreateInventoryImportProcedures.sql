USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[ImportInventoryReport]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[ImportInventoryReport];
END;
GO

CREATE PROCEDURE [ebay].[ImportInventoryReport]
    @Source [ebay].[sourceName] = N'SellFeedApi',
    @FeedType [ebay].[feedType] = N'LMS_ACTIVE_INVENTORY_REPORT',
    @TaskId [ebay].[taskId] = NULL,
    @EbayAck [ebay].[statusCode] = N'Success',
    @SourceFileName [ebay].[fileName],
    @SourceContent varbinary(max),
    @SourceFileSha256 [ebay].[sha256],
    @StartedAtLocal [ebay].[fechaLocal] = NULL,
    @CompletedAtLocal [ebay].[fechaLocal] = NULL,
    @ImportedAtLocal [ebay].[fechaLocal] = NULL,
    @Listings [ebay].[InventoryListingSnapshotImportTableType] READONLY,
    @Sellables [ebay].[InventorySellableSnapshotImportTableType] READONLY,
    @VariationSpecifics [ebay].[InventoryVariationSpecificSnapshotImportTableType] READONLY,
    @ImportBatchId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowLocal datetime2(7) = SYSDATETIME();
    DECLARE @EffectiveImportedAtLocal datetime2(7) = ISNULL(@ImportedAtLocal, @NowLocal);
    DECLARE @EffectiveStartedAtLocal datetime2(7) = ISNULL(@StartedAtLocal, @EffectiveImportedAtLocal);
    DECLARE @EffectiveCompletedAtLocal datetime2(7) = ISNULL(@CompletedAtLocal, @EffectiveImportedAtLocal);
    DECLARE @RetentionUntilLocal datetime2(7) = DATEADD(day, 60, @EffectiveImportedAtLocal);
    DECLARE @EffectiveTaskId nvarchar(128) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(128), @TaskId))), N'');
    DECLARE @ExistingByTaskId bigint = NULL;
    DECLARE @ExistingByHash bigint = NULL;
    DECLARE @SkuDetailsCount int = (SELECT COUNT(1) FROM @Listings);
    DECLARE @VariationCount int = (SELECT COUNT(1) FROM @Sellables WHERE [IsVariation] = 1);

    IF @SourceFileName IS NULL OR LTRIM(RTRIM(CONVERT(nvarchar(260), @SourceFileName))) = N''
    BEGIN
        THROW 51000, 'SourceFileName is required.', 1;
    END;

    IF @SourceContent IS NULL OR DATALENGTH(@SourceContent) = 0
    BEGIN
        THROW 51001, 'SourceContent is required.', 1;
    END;

    IF @SourceFileSha256 IS NULL OR LEN(@SourceFileSha256) <> 64
    BEGIN
        THROW 51002, 'SourceFileSha256 must contain 64 characters.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Sellables AS s
        LEFT JOIN @Listings AS l
            ON l.[ListingRowNumber] = s.[ListingRowNumber]
        WHERE l.[ListingRowNumber] IS NULL
    )
    BEGIN
        THROW 51003, 'Sellables contain a ListingRowNumber that does not exist in Listings.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Sellables AS s
        INNER JOIN @Listings AS l
            ON l.[ListingRowNumber] = s.[ListingRowNumber]
        WHERE s.[EbayItemId] <> l.[EbayItemId]
    )
    BEGIN
        THROW 51004, 'Sellable EbayItemId must match its listing EbayItemId.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @VariationSpecifics AS vs
        LEFT JOIN @Sellables AS s
            ON s.[SellableRowNumber] = vs.[SellableRowNumber]
        WHERE s.[SellableRowNumber] IS NULL
    )
    BEGIN
        THROW 51005, 'Variation specifics contain a SellableRowNumber that does not exist in Sellables.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @VariationSpecifics AS vs
        INNER JOIN @Sellables AS s
            ON s.[SellableRowNumber] = vs.[SellableRowNumber]
        WHERE s.[IsVariation] = 0
    )
    BEGIN
        THROW 51006, 'Variation specifics can only be linked to variation sellables.', 1;
    END;

    SELECT @ExistingByHash = [Id]
    FROM [ebay].[InventoryImportBatch]
    WHERE [SourceFileSha256] = @SourceFileSha256;

    IF @EffectiveTaskId IS NOT NULL
    BEGIN
        SELECT @ExistingByTaskId = [Id]
        FROM [ebay].[InventoryImportBatch]
        WHERE [TaskId] = @EffectiveTaskId;
    END;

    IF @ExistingByHash IS NOT NULL
       AND @ExistingByTaskId IS NOT NULL
       AND @ExistingByHash <> @ExistingByTaskId
    BEGIN
        THROW 51007, 'TaskId and SourceFileSha256 match different import batches.', 1;
    END;

    SET @ImportBatchId = ISNULL(@ExistingByTaskId, @ExistingByHash);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @ImportBatchId IS NULL
        BEGIN
            INSERT INTO [ebay].[InventoryImportBatch]
            (
                [Source],
                [FeedType],
                [TaskId],
                [Status],
                [EbayAck],
                [SourceFileName],
                [SourceContent],
                [SourceFileSha256],
                [StartedAtLocal],
                [CompletedAtLocal],
                [ImportedAtLocal],
                [RetentionUntilLocal],
                [SkuDetailsCount],
                [VariationCount]
            )
            VALUES
            (
                @Source,
                @FeedType,
                @EffectiveTaskId,
                N'Imported',
                @EbayAck,
                @SourceFileName,
                @SourceContent,
                @SourceFileSha256,
                @EffectiveStartedAtLocal,
                @EffectiveCompletedAtLocal,
                @EffectiveImportedAtLocal,
                @RetentionUntilLocal,
                @SkuDetailsCount,
                @VariationCount
            );

            SET @ImportBatchId = CONVERT(bigint, SCOPE_IDENTITY());
        END;
        ELSE
        BEGIN
            UPDATE [ebay].[InventoryImportBatch]
            SET
                [Source] = @Source,
                [FeedType] = @FeedType,
                [TaskId] = ISNULL(@EffectiveTaskId, [TaskId]),
                [Status] = N'Imported',
                [EbayAck] = @EbayAck,
                [SourceFileName] = @SourceFileName,
                [SourceContent] = @SourceContent,
                [SourceFileSha256] = @SourceFileSha256,
                [StartedAtLocal] = @EffectiveStartedAtLocal,
                [CompletedAtLocal] = @EffectiveCompletedAtLocal,
                [ImportedAtLocal] = @EffectiveImportedAtLocal,
                [RetentionUntilLocal] = @RetentionUntilLocal,
                [SkuDetailsCount] = @SkuDetailsCount,
                [VariationCount] = @VariationCount,
                [ErrorMessage] = NULL
            WHERE [Id] = @ImportBatchId;
        END;

        DELETE target
        FROM [ebay].[InventoryListingSnapshot] AS target
        WHERE target.[ImportBatchId] = @ImportBatchId
          AND NOT EXISTS
          (
              SELECT 1
              FROM @Listings AS source
              WHERE source.[EbayItemId] = target.[EbayItemId]
          );

        MERGE [ebay].[InventoryListingSnapshot] AS target
        USING @Listings AS source
            ON target.[ImportBatchId] = @ImportBatchId
           AND target.[EbayItemId] = source.[EbayItemId]
        WHEN MATCHED THEN
            UPDATE SET
                [ParentSku] = source.[ParentSku],
                [ReportedParentQuantity] = source.[ReportedParentQuantity],
                [HasVariations] = source.[HasVariations],
                [ParentPriceAmount] = source.[ParentPriceAmount],
                [ParentPriceCurrency] = source.[ParentPriceCurrency]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [ImportBatchId],
                [EbayItemId],
                [ParentSku],
                [ReportedParentQuantity],
                [HasVariations],
                [ParentPriceAmount],
                [ParentPriceCurrency]
            )
            VALUES
            (
                @ImportBatchId,
                source.[EbayItemId],
                source.[ParentSku],
                source.[ReportedParentQuantity],
                source.[HasVariations],
                source.[ParentPriceAmount],
                source.[ParentPriceCurrency]
            );

        DECLARE @ListingMap TABLE
        (
            [ListingRowNumber] int NOT NULL PRIMARY KEY,
            [ListingSnapshotId] bigint NOT NULL,
            [EbayItemId] nvarchar(32) NOT NULL
        );

        INSERT INTO @ListingMap
        (
            [ListingRowNumber],
            [ListingSnapshotId],
            [EbayItemId]
        )
        SELECT
            source.[ListingRowNumber],
            target.[Id],
            target.[EbayItemId]
        FROM @Listings AS source
        INNER JOIN [ebay].[InventoryListingSnapshot] AS target
            ON target.[ImportBatchId] = @ImportBatchId
           AND target.[EbayItemId] = source.[EbayItemId];

        DELETE target
        FROM [ebay].[InventorySellableSnapshot] AS target
        WHERE target.[ImportBatchId] = @ImportBatchId
          AND NOT EXISTS
          (
              SELECT 1
              FROM @Sellables AS source
              WHERE source.[EbayItemId] = target.[EbayItemId]
                AND source.[SellableSku] = target.[SellableSku]
          );

        MERGE [ebay].[InventorySellableSnapshot] AS target
        USING
        (
            SELECT
                source.[SellableRowNumber],
                map.[ListingSnapshotId],
                source.[EbayItemId],
                source.[ParentSku],
                source.[SellableSku],
                source.[IsVariation],
                source.[ReportedQuantity],
                source.[PriceAmount],
                source.[PriceCurrency]
            FROM @Sellables AS source
            INNER JOIN @ListingMap AS map
                ON map.[ListingRowNumber] = source.[ListingRowNumber]
        ) AS source
            ON target.[ImportBatchId] = @ImportBatchId
           AND target.[EbayItemId] = source.[EbayItemId]
           AND target.[SellableSku] = source.[SellableSku]
        WHEN MATCHED THEN
            UPDATE SET
                [ListingSnapshotId] = source.[ListingSnapshotId],
                [ParentSku] = source.[ParentSku],
                [IsVariation] = source.[IsVariation],
                [ReportedQuantity] = source.[ReportedQuantity],
                [PriceAmount] = source.[PriceAmount],
                [PriceCurrency] = source.[PriceCurrency]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [ImportBatchId],
                [ListingSnapshotId],
                [EbayItemId],
                [ParentSku],
                [SellableSku],
                [IsVariation],
                [ReportedQuantity],
                [PriceAmount],
                [PriceCurrency]
            )
            VALUES
            (
                @ImportBatchId,
                source.[ListingSnapshotId],
                source.[EbayItemId],
                source.[ParentSku],
                source.[SellableSku],
                source.[IsVariation],
                source.[ReportedQuantity],
                source.[PriceAmount],
                source.[PriceCurrency]
            );

        DECLARE @SellableMap TABLE
        (
            [SellableRowNumber] int NOT NULL PRIMARY KEY,
            [SellableSnapshotId] bigint NOT NULL,
            [EbayItemId] nvarchar(32) NOT NULL,
            [SellableSku] nvarchar(64) NOT NULL
        );

        INSERT INTO @SellableMap
        (
            [SellableRowNumber],
            [SellableSnapshotId],
            [EbayItemId],
            [SellableSku]
        )
        SELECT
            source.[SellableRowNumber],
            target.[Id],
            target.[EbayItemId],
            target.[SellableSku]
        FROM @Sellables AS source
        INNER JOIN [ebay].[InventorySellableSnapshot] AS target
            ON target.[ImportBatchId] = @ImportBatchId
           AND target.[EbayItemId] = source.[EbayItemId]
           AND target.[SellableSku] = source.[SellableSku];

        DELETE target
        FROM [ebay].[InventoryVariationSpecificSnapshot] AS target
        INNER JOIN [ebay].[InventorySellableSnapshot] AS sellable
            ON sellable.[Id] = target.[SellableSnapshotId]
           AND sellable.[ImportBatchId] = @ImportBatchId
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM @VariationSpecifics AS source
            INNER JOIN @SellableMap AS map
                ON map.[SellableRowNumber] = source.[SellableRowNumber]
            WHERE map.[SellableSnapshotId] = target.[SellableSnapshotId]
              AND source.[SortOrder] = target.[SortOrder]
        );

        MERGE [ebay].[InventoryVariationSpecificSnapshot] AS target
        USING
        (
            SELECT
                map.[SellableSnapshotId],
                source.[SortOrder],
                source.[Name],
                source.[Value]
            FROM @VariationSpecifics AS source
            INNER JOIN @SellableMap AS map
                ON map.[SellableRowNumber] = source.[SellableRowNumber]
        ) AS source
            ON target.[SellableSnapshotId] = source.[SellableSnapshotId]
           AND target.[SortOrder] = source.[SortOrder]
        WHEN MATCHED THEN
            UPDATE SET
                [Name] = source.[Name],
                [Value] = source.[Value]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [SellableSnapshotId],
                [Name],
                [Value],
                [SortOrder]
            )
            VALUES
            (
                source.[SellableSnapshotId],
                source.[Name],
                source.[Value],
                source.[SortOrder]
            );

        MERGE [ebay].[InventorySellableEfaValidation] AS target
        USING
        (
            SELECT [SellableSnapshotId]
            FROM @SellableMap
        ) AS source
            ON target.[SellableSnapshotId] = source.[SellableSnapshotId]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [SellableSnapshotId],
                [ValidationStatus]
            )
            VALUES
            (
                source.[SellableSnapshotId],
                N'Pending'
            );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;

    SELECT
        @ImportBatchId AS [ImportBatchId],
        @SkuDetailsCount AS [SkuDetailsCount],
        @VariationCount AS [VariationCount],
        (SELECT COUNT(1) FROM @Sellables) AS [SellableCount],
        (SELECT COUNT(1) FROM @VariationSpecifics) AS [VariationSpecificCount];
END;
GO

IF OBJECT_ID(N'[ebay].[UpsertInventorySellableEfaValidation]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[UpsertInventorySellableEfaValidation];
END;
GO

CREATE PROCEDURE [ebay].[UpsertInventorySellableEfaValidation]
    @ImportBatchId bigint,
    @Validations [ebay].[InventorySellableEfaValidationImportTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowLocal datetime2(7) = SYSDATETIME();

    IF NOT EXISTS (SELECT 1 FROM [ebay].[InventoryImportBatch] WHERE [Id] = @ImportBatchId)
    BEGIN
        THROW 51100, 'ImportBatchId does not exist.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Validations AS source
        LEFT JOIN [ebay].[InventorySellableSnapshot] AS sellable
            ON sellable.[ImportBatchId] = @ImportBatchId
           AND sellable.[EbayItemId] = source.[EbayItemId]
           AND sellable.[SellableSku] = source.[SellableSku]
        WHERE sellable.[Id] IS NULL
    )
    BEGIN
        THROW 51101, 'Validation rows reference sellables that do not exist in the selected import batch.', 1;
    END;

    MERGE [ebay].[InventorySellableEfaValidation] AS target
    USING
    (
        SELECT
            sellable.[Id] AS [SellableSnapshotId],
            source.[ParsedEfaItem],
            source.[ParsedEfaSalesUnit],
            source.[ValidationStatus],
            source.[EfaItemFound],
            source.[EfaSalesUnitFound],
            source.[EfaItemIsActive],
            source.[Message]
        FROM @Validations AS source
        INNER JOIN [ebay].[InventorySellableSnapshot] AS sellable
            ON sellable.[ImportBatchId] = @ImportBatchId
           AND sellable.[EbayItemId] = source.[EbayItemId]
           AND sellable.[SellableSku] = source.[SellableSku]
    ) AS source
        ON target.[SellableSnapshotId] = source.[SellableSnapshotId]
    WHEN MATCHED THEN
        UPDATE SET
            [ParsedEfaItem] = source.[ParsedEfaItem],
            [ParsedEfaSalesUnit] = source.[ParsedEfaSalesUnit],
            [ValidationStatus] = source.[ValidationStatus],
            [EfaItemFound] = source.[EfaItemFound],
            [EfaSalesUnitFound] = source.[EfaSalesUnitFound],
            [EfaItemIsActive] = source.[EfaItemIsActive],
            [ValidatedAtLocal] = @NowLocal,
            [Message] = source.[Message]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            [SellableSnapshotId],
            [ParsedEfaItem],
            [ParsedEfaSalesUnit],
            [ValidationStatus],
            [EfaItemFound],
            [EfaSalesUnitFound],
            [EfaItemIsActive],
            [ValidatedAtLocal],
            [Message]
        )
        VALUES
        (
            source.[SellableSnapshotId],
            source.[ParsedEfaItem],
            source.[ParsedEfaSalesUnit],
            source.[ValidationStatus],
            source.[EfaItemFound],
            source.[EfaSalesUnitFound],
            source.[EfaItemIsActive],
            @NowLocal,
            source.[Message]
        );
END;
GO

IF OBJECT_ID(N'[ebay].[ReplaceArticleCompatibilityIssues]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[ReplaceArticleCompatibilityIssues];
END;
GO

CREATE PROCEDURE [ebay].[ReplaceArticleCompatibilityIssues]
    @ImportBatchId bigint,
    @Issues [ebay].[ArticleCompatibilityIssueImportTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowLocal datetime2(7) = SYSDATETIME();

    IF NOT EXISTS (SELECT 1 FROM [ebay].[InventoryImportBatch] WHERE [Id] = @ImportBatchId)
    BEGIN
        THROW 51200, 'ImportBatchId does not exist.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Issues AS source
        LEFT JOIN [ebay].[InventorySellableSnapshot] AS sellable
            ON sellable.[ImportBatchId] = @ImportBatchId
           AND sellable.[EbayItemId] = source.[EbayItemId]
           AND sellable.[SellableSku] = source.[SellableSku]
        WHERE sellable.[Id] IS NULL
    )
    BEGIN
        THROW 51201, 'Issue rows reference sellables that do not exist in the selected import batch.', 1;
    END;

    MERGE [ebay].[ArticleCompatibilityIssue] AS target
    USING @Issues AS source
        ON target.[ImportBatchId] = @ImportBatchId
       AND target.[EbayItemId] = source.[EbayItemId]
       AND target.[SellableSku] = source.[SellableSku]
       AND target.[IssueType] = source.[IssueType]
       AND target.[ResolvedAtLocal] IS NULL
    WHEN MATCHED THEN
        UPDATE SET
            [ParsedEfaItem] = source.[ParsedEfaItem],
            [ParsedEfaSalesUnit] = source.[ParsedEfaSalesUnit],
            [Severity] = source.[Severity],
            [Message] = source.[Message]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT
        (
            [ImportBatchId],
            [EbayItemId],
            [SellableSku],
            [ParsedEfaItem],
            [ParsedEfaSalesUnit],
            [IssueType],
            [Severity],
            [Message],
            [DetectedAtLocal]
        )
        VALUES
        (
            @ImportBatchId,
            source.[EbayItemId],
            source.[SellableSku],
            source.[ParsedEfaItem],
            source.[ParsedEfaSalesUnit],
            source.[IssueType],
            source.[Severity],
            source.[Message],
            @NowLocal
        )
    WHEN NOT MATCHED BY SOURCE
         AND target.[ImportBatchId] = @ImportBatchId
         AND target.[ResolvedAtLocal] IS NULL THEN
        UPDATE SET
            [ResolvedAtLocal] = @NowLocal;
END;
GO
