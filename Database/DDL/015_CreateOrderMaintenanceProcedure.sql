USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[CleanupOrderHistory]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[CleanupOrderHistory];
END;
GO

CREATE PROCEDURE [ebay].[CleanupOrderHistory]
    @RetentionCutoffLocal datetime2(7) = NULL,
    @ClearedSourceContents int OUTPUT,
    @DeletedOrders int OUTPUT,
    @DeletedImportBatches int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EffectiveCutoffLocal datetime2(7) = ISNULL(@RetentionCutoffLocal, SYSDATETIME());

    BEGIN TRANSACTION;

    UPDATE [ebay].[OrderImportBatch]
    SET [SourceContent] = NULL
    WHERE [SourceContent] IS NOT NULL
      AND [SourceContentRetentionUntilLocal] < @EffectiveCutoffLocal;

    SET @ClearedSourceContents = @@ROWCOUNT;

    DELETE FROM [ebay].[EbayOrder]
    WHERE [RetentionUntilLocal] < @EffectiveCutoffLocal;

    SET @DeletedOrders = @@ROWCOUNT;

    DELETE batch
    FROM [ebay].[OrderImportBatch] AS batch
    WHERE batch.[RetentionUntilLocal] < @EffectiveCutoffLocal
      AND NOT EXISTS
      (
          SELECT 1
          FROM [ebay].[EbayOrder] AS o
          WHERE o.[LastImportBatchId] = batch.[Id]
      );

    SET @DeletedImportBatches = @@ROWCOUNT;

    COMMIT TRANSACTION;
END;
GO
