USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[CleanupInventoryHistory]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[CleanupInventoryHistory];
END;
GO

CREATE PROCEDURE [ebay].[CleanupInventoryHistory]
    @RetentionCutoffLocal datetime2(7) = NULL,
    @DeletedBatches int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EffectiveCutoffLocal datetime2(7) = ISNULL(@RetentionCutoffLocal, SYSDATETIME());
    DECLARE @DeletedBatchIds TABLE ([Id] bigint NOT NULL);

    DELETE FROM [ebay].[InventoryImportBatch]
    OUTPUT deleted.[Id] INTO @DeletedBatchIds ([Id])
    WHERE [RetentionUntilLocal] < @EffectiveCutoffLocal;

    SELECT @DeletedBatches = COUNT(1)
    FROM @DeletedBatchIds;
END;
GO
