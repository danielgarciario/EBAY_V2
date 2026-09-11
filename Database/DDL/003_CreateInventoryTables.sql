USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[InventoryImportBatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[InventoryImportBatch]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Source] [ebay].[sourceName] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_Source] DEFAULT (N'SellFeedApi'),
        [FeedType] [ebay].[feedType] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_FeedType] DEFAULT (N'LMS_ACTIVE_INVENTORY_REPORT'),
        [TaskId] [ebay].[taskId] NULL,
        [Status] [ebay].[statusCode] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_Status] DEFAULT (N'Created'),
        [EbayAck] [ebay].[statusCode] NULL,
        [SourceFileName] [ebay].[fileName] NOT NULL,
        [SourceContent] varbinary(max) NOT NULL,
        [SourceFileSha256] [ebay].[sha256] NOT NULL,
        [StartedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_StartedAtLocal] DEFAULT (SYSDATETIME()),
        [CompletedAtLocal] [ebay].[fechaLocal] NULL,
        [ImportedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_ImportedAtLocal] DEFAULT (SYSDATETIME()),
        [RetentionUntilLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_InventoryImportBatch_RetentionUntilLocal] DEFAULT (DATEADD(day, 60, SYSDATETIME())),
        [SkuDetailsCount] int NULL,
        [VariationCount] int NULL,
        [ErrorMessage] nvarchar(max) NULL,
        CONSTRAINT [PK_InventoryImportBatch] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_InventoryImportBatch_SourceFileSha256] CHECK (LEN([SourceFileSha256]) = 64),
        CONSTRAINT [CK_InventoryImportBatch_SkuDetailsCount] CHECK ([SkuDetailsCount] IS NULL OR [SkuDetailsCount] >= 0),
        CONSTRAINT [CK_InventoryImportBatch_VariationCount] CHECK ([VariationCount] IS NULL OR [VariationCount] >= 0),
        CONSTRAINT [CK_InventoryImportBatch_RetentionUntilLocal] CHECK ([RetentionUntilLocal] >= [ImportedAtLocal])
    );
END;
GO

IF OBJECT_ID(N'[ebay].[InventoryListingSnapshot]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[InventoryListingSnapshot]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [ImportBatchId] bigint NOT NULL,
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [ParentSku] [ebay].[SKU] NOT NULL,
        [ReportedParentQuantity] [ebay].[cantidad] NOT NULL,
        [HasVariations] bit NOT NULL,
        [ParentPriceAmount] [ebay].[dinero] NULL,
        [ParentPriceCurrency] [ebay].[currencyCode] NULL,
        CONSTRAINT [PK_InventoryListingSnapshot] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InventoryListingSnapshot_InventoryImportBatch]
            FOREIGN KEY ([ImportBatchId])
            REFERENCES [ebay].[InventoryImportBatch] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_InventoryListingSnapshot_Id_Batch]
            UNIQUE ([Id], [ImportBatchId]),
        CONSTRAINT [UQ_InventoryListingSnapshot_Batch_Item]
            UNIQUE ([ImportBatchId], [EbayItemId]),
        CONSTRAINT [CK_InventoryListingSnapshot_ReportedParentQuantity]
            CHECK ([ReportedParentQuantity] >= 0),
        CONSTRAINT [CK_InventoryListingSnapshot_ParentPriceAmount]
            CHECK ([ParentPriceAmount] IS NULL OR [ParentPriceAmount] >= 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[InventorySellableSnapshot]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[InventorySellableSnapshot]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [ImportBatchId] bigint NOT NULL,
        [ListingSnapshotId] bigint NOT NULL,
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [ParentSku] [ebay].[SKU] NOT NULL,
        [SellableSku] [ebay].[SKU] NOT NULL,
        [IsVariation] bit NOT NULL,
        [ReportedQuantity] [ebay].[cantidad] NOT NULL,
        [PriceAmount] [ebay].[dinero] NOT NULL,
        [PriceCurrency] [ebay].[currencyCode] NOT NULL,
        CONSTRAINT [PK_InventorySellableSnapshot] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InventorySellableSnapshot_InventoryListingSnapshot]
            FOREIGN KEY ([ListingSnapshotId], [ImportBatchId])
            REFERENCES [ebay].[InventoryListingSnapshot] ([Id], [ImportBatchId])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_InventorySellableSnapshot_Batch_Item_Sku]
            UNIQUE ([ImportBatchId], [EbayItemId], [SellableSku]),
        CONSTRAINT [CK_InventorySellableSnapshot_ReportedQuantity]
            CHECK ([ReportedQuantity] >= 0),
        CONSTRAINT [CK_InventorySellableSnapshot_PriceAmount]
            CHECK ([PriceAmount] >= 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[InventoryVariationSpecificSnapshot]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[InventoryVariationSpecificSnapshot]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [SellableSnapshotId] bigint NOT NULL,
        [Name] [ebay].[variationSpecificName] NOT NULL,
        [Value] [ebay].[variationSpecificValue] NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_InventoryVariationSpecificSnapshot] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InventoryVariationSpecificSnapshot_InventorySellableSnapshot]
            FOREIGN KEY ([SellableSnapshotId])
            REFERENCES [ebay].[InventorySellableSnapshot] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_InventoryVariationSpecificSnapshot_Sellable_SortOrder]
            UNIQUE ([SellableSnapshotId], [SortOrder]),
        CONSTRAINT [CK_InventoryVariationSpecificSnapshot_SortOrder]
            CHECK ([SortOrder] > 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[InventorySellableEfaValidation]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[InventorySellableEfaValidation]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [SellableSnapshotId] bigint NOT NULL,
        [ParsedEfaItem] [ebay].[efaItem] NULL,
        [ParsedEfaSalesUnit] [ebay].[efaSalesUnit] NULL,
        [ValidationStatus] [ebay].[validationStatus] NOT NULL
            CONSTRAINT [DF_InventorySellableEfaValidation_ValidationStatus] DEFAULT (N'Pending'),
        [EfaItemFound] bit NULL,
        [EfaSalesUnitFound] bit NULL,
        [EfaItemIsActive] bit NULL,
        [ValidatedAtLocal] [ebay].[fechaLocal] NULL,
        [Message] nvarchar(max) NULL,
        CONSTRAINT [PK_InventorySellableEfaValidation] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_InventorySellableEfaValidation_InventorySellableSnapshot]
            FOREIGN KEY ([SellableSnapshotId])
            REFERENCES [ebay].[InventorySellableSnapshot] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_InventorySellableEfaValidation_Sellable]
            UNIQUE ([SellableSnapshotId]),
        CONSTRAINT [CK_InventorySellableEfaValidation_ValidationStatus]
            CHECK ([ValidationStatus] IN (N'Pending', N'Valid', N'InvalidSkuFormat', N'MissingInEfa', N'MissingSalesUnitInEfa', N'InactiveInEfa', N'Failed'))
    );
END;
GO

IF OBJECT_ID(N'[ebay].[ArticleCompatibilityIssue]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[ArticleCompatibilityIssue]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [ImportBatchId] bigint NOT NULL,
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [SellableSku] [ebay].[SKU] NOT NULL,
        [ParsedEfaItem] [ebay].[efaItem] NULL,
        [ParsedEfaSalesUnit] [ebay].[efaSalesUnit] NULL,
        [IssueType] [ebay].[issueType] NOT NULL,
        [Severity] [ebay].[severity] NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [DetectedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_ArticleCompatibilityIssue_DetectedAtLocal] DEFAULT (SYSDATETIME()),
        [ResolvedAtLocal] [ebay].[fechaLocal] NULL,
        CONSTRAINT [PK_ArticleCompatibilityIssue] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ArticleCompatibilityIssue_InventoryImportBatch]
            FOREIGN KEY ([ImportBatchId])
            REFERENCES [ebay].[InventoryImportBatch] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_ArticleCompatibilityIssue_Severity]
            CHECK ([Severity] IN (N'Info', N'Warning', N'Error')),
        CONSTRAINT [CK_ArticleCompatibilityIssue_ResolvedAtLocal]
            CHECK ([ResolvedAtLocal] IS NULL OR [ResolvedAtLocal] >= [DetectedAtLocal])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventoryImportBatch_RetentionUntilLocal' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryImportBatch]'))
BEGIN
    CREATE INDEX [IX_InventoryImportBatch_RetentionUntilLocal]
        ON [ebay].[InventoryImportBatch] ([RetentionUntilLocal]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_InventoryImportBatch_SourceFileSha256' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryImportBatch]'))
BEGIN
    CREATE UNIQUE INDEX [UX_InventoryImportBatch_SourceFileSha256]
        ON [ebay].[InventoryImportBatch] ([SourceFileSha256]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_InventoryImportBatch_TaskId' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryImportBatch]'))
BEGIN
    CREATE UNIQUE INDEX [UX_InventoryImportBatch_TaskId]
        ON [ebay].[InventoryImportBatch] ([TaskId])
        WHERE [TaskId] IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventoryImportBatch_Status_ImportedAtLocal' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryImportBatch]'))
BEGIN
    CREATE INDEX [IX_InventoryImportBatch_Status_ImportedAtLocal]
        ON [ebay].[InventoryImportBatch] ([Status], [ImportedAtLocal] DESC, [Id] DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventoryListingSnapshot_EbayItemId' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryListingSnapshot]'))
BEGIN
    CREATE INDEX [IX_InventoryListingSnapshot_EbayItemId]
        ON [ebay].[InventoryListingSnapshot] ([EbayItemId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventorySellableSnapshot_SellableSku' AND [object_id] = OBJECT_ID(N'[ebay].[InventorySellableSnapshot]'))
BEGIN
    CREATE INDEX [IX_InventorySellableSnapshot_SellableSku]
        ON [ebay].[InventorySellableSnapshot] ([SellableSku]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventorySellableSnapshot_Item_Sku' AND [object_id] = OBJECT_ID(N'[ebay].[InventorySellableSnapshot]'))
BEGIN
    CREATE INDEX [IX_InventorySellableSnapshot_Item_Sku]
        ON [ebay].[InventorySellableSnapshot] ([EbayItemId], [SellableSku]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventoryVariationSpecificSnapshot_Name_Value' AND [object_id] = OBJECT_ID(N'[ebay].[InventoryVariationSpecificSnapshot]'))
BEGIN
    CREATE INDEX [IX_InventoryVariationSpecificSnapshot_Name_Value]
        ON [ebay].[InventoryVariationSpecificSnapshot] ([Name], [Value]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_InventorySellableEfaValidation_Status' AND [object_id] = OBJECT_ID(N'[ebay].[InventorySellableEfaValidation]'))
BEGIN
    CREATE INDEX [IX_InventorySellableEfaValidation_Status]
        ON [ebay].[InventorySellableEfaValidation] ([ValidationStatus]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ArticleCompatibilityIssue_Open' AND [object_id] = OBJECT_ID(N'[ebay].[ArticleCompatibilityIssue]'))
BEGIN
    CREATE INDEX [IX_ArticleCompatibilityIssue_Open]
        ON [ebay].[ArticleCompatibilityIssue] ([ResolvedAtLocal], [Severity], [IssueType]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_ArticleCompatibilityIssue_Open_Batch_Item_Sku_Type' AND [object_id] = OBJECT_ID(N'[ebay].[ArticleCompatibilityIssue]'))
BEGIN
    CREATE UNIQUE INDEX [UX_ArticleCompatibilityIssue_Open_Batch_Item_Sku_Type]
        ON [ebay].[ArticleCompatibilityIssue] ([ImportBatchId], [EbayItemId], [SellableSku], [IssueType])
        WHERE [ResolvedAtLocal] IS NULL;
END;
GO
