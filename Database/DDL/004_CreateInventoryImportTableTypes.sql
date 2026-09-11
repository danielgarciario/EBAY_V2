USE [EBAY];
GO

IF TYPE_ID(N'ebay.InventoryListingSnapshotImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[InventoryListingSnapshotImportTableType] AS TABLE
    (
        [ListingRowNumber] int NOT NULL,
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [ParentSku] [ebay].[SKU] NOT NULL,
        [ReportedParentQuantity] [ebay].[cantidad] NOT NULL,
        [HasVariations] bit NOT NULL,
        [ParentPriceAmount] [ebay].[dinero] NULL,
        [ParentPriceCurrency] [ebay].[currencyCode] NULL,
        PRIMARY KEY CLUSTERED ([ListingRowNumber]),
        UNIQUE ([EbayItemId])
    );');
END;
GO

IF TYPE_ID(N'ebay.InventorySellableSnapshotImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[InventorySellableSnapshotImportTableType] AS TABLE
    (
        [SellableRowNumber] int NOT NULL,
        [ListingRowNumber] int NOT NULL,
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [ParentSku] [ebay].[SKU] NOT NULL,
        [SellableSku] [ebay].[SKU] NOT NULL,
        [IsVariation] bit NOT NULL,
        [ReportedQuantity] [ebay].[cantidad] NOT NULL,
        [PriceAmount] [ebay].[dinero] NOT NULL,
        [PriceCurrency] [ebay].[currencyCode] NOT NULL,
        PRIMARY KEY CLUSTERED ([SellableRowNumber]),
        UNIQUE ([EbayItemId], [SellableSku])
    );');
END;
GO

IF TYPE_ID(N'ebay.InventoryVariationSpecificSnapshotImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[InventoryVariationSpecificSnapshotImportTableType] AS TABLE
    (
        [SellableRowNumber] int NOT NULL,
        [SortOrder] int NOT NULL,
        [Name] [ebay].[variationSpecificName] NOT NULL,
        [Value] [ebay].[variationSpecificValue] NOT NULL,
        PRIMARY KEY CLUSTERED ([SellableRowNumber], [SortOrder])
    );');
END;
GO

IF TYPE_ID(N'ebay.InventorySellableEfaValidationImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[InventorySellableEfaValidationImportTableType] AS TABLE
    (
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [SellableSku] [ebay].[SKU] NOT NULL,
        [ParsedEfaItem] [ebay].[efaItem] NULL,
        [ParsedEfaSalesUnit] [ebay].[efaSalesUnit] NULL,
        [ValidationStatus] [ebay].[validationStatus] NOT NULL,
        [EfaItemFound] bit NULL,
        [EfaSalesUnitFound] bit NULL,
        [EfaItemIsActive] bit NULL,
        [Message] nvarchar(max) NULL,
        PRIMARY KEY CLUSTERED ([EbayItemId], [SellableSku])
    );');
END;
GO

IF TYPE_ID(N'ebay.ArticleCompatibilityIssueImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[ArticleCompatibilityIssueImportTableType] AS TABLE
    (
        [EbayItemId] [ebay].[eBayItemId] NOT NULL,
        [SellableSku] [ebay].[SKU] NOT NULL,
        [ParsedEfaItem] [ebay].[efaItem] NULL,
        [ParsedEfaSalesUnit] [ebay].[efaSalesUnit] NULL,
        [IssueType] [ebay].[issueType] NOT NULL,
        [Severity] [ebay].[severity] NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        PRIMARY KEY CLUSTERED ([EbayItemId], [SellableSku], [IssueType])
    );');
END;
GO
