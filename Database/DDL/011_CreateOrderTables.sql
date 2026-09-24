USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[OrderImportBatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[OrderImportBatch]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [Source] [ebay].[orderImportSource] NOT NULL,
        [SourceReference] nvarchar(2048) NULL,
        [SourceContent] nvarchar(max) NULL,
        [SourceContentSha256] [ebay].[sha256] NULL,
        [StartedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_OrderImportBatch_StartedAtLocal] DEFAULT (SYSDATETIME()),
        [CompletedAtLocal] [ebay].[fechaLocal] NULL,
        [ImportedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_OrderImportBatch_ImportedAtLocal] DEFAULT (SYSDATETIME()),
        [SourceContentRetentionUntilLocal] [ebay].[fechaLocal] NOT NULL,
        [RetentionUntilLocal] [ebay].[fechaLocal] NOT NULL,
        [OrderCount] int NOT NULL,
        CONSTRAINT [PK_OrderImportBatch] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_OrderImportBatch_Source]
            CHECK ([Source] IN (N'Polling', N'Webhook')),
        CONSTRAINT [CK_OrderImportBatch_SourceContent]
            CHECK ([SourceContent] IS NULL OR ISJSON([SourceContent]) = 1),
        CONSTRAINT [CK_OrderImportBatch_SourceContentSha256]
            CHECK ([SourceContentSha256] IS NULL OR LEN([SourceContentSha256]) = 64),
        CONSTRAINT [CK_OrderImportBatch_OrderCount]
            CHECK ([OrderCount] >= 0),
        CONSTRAINT [CK_OrderImportBatch_SourceContentRetention]
            CHECK ([SourceContentRetentionUntilLocal] >= [ImportedAtLocal]),
        CONSTRAINT [CK_OrderImportBatch_Retention]
            CHECK ([RetentionUntilLocal] >= [SourceContentRetentionUntilLocal])
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrder]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrder]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [EbayOrderId] [ebay].[eBayOrderId] NOT NULL,
        [LegacyOrderId] [ebay].[eBayOrderId] NULL,
        [SalesRecordReference] nvarchar(64) NULL,
        [SellerId] nvarchar(128) NOT NULL,
        [BuyerUsername] nvarchar(128) NOT NULL,
        [CreationDateLocal] [ebay].[fechaLocal] NOT NULL,
        [LastModifiedDateLocal] [ebay].[fechaLocal] NOT NULL,
        [OrderFulfillmentStatus] [ebay].[statusCode] NOT NULL,
        [OrderPaymentStatus] [ebay].[statusCode] NOT NULL,
        [CancelState] [ebay].[statusCode] NOT NULL,
        [PriceSubtotalAmount] [ebay].[dinero] NULL,
        [PriceSubtotalCurrency] [ebay].[currencyCode] NULL,
        [PriceDiscountAmount] [ebay].[dinero] NULL,
        [DeliveryCostAmount] [ebay].[dinero] NULL,
        [DeliveryDiscountAmount] [ebay].[dinero] NULL,
        [TotalAmount] [ebay].[dinero] NOT NULL,
        [TotalCurrency] [ebay].[currencyCode] NOT NULL,
        [TotalFeeBasisAmount] [ebay].[dinero] NULL,
        [TotalFeeBasisCurrency] [ebay].[currencyCode] NULL,
        [TotalMarketplaceFeeAmount] [ebay].[dinero] NULL,
        [TotalMarketplaceFeeCurrency] [ebay].[currencyCode] NULL,
        [BuyerTaxCity] nvarchar(128) NULL,
        [BuyerTaxPostalCode] nvarchar(32) NULL,
        [BuyerTaxCountryCode] char(2) NULL,
        [BuyerRegistrationFullName] nvarchar(256) NULL,
        [BuyerRegistrationAddressLine1] nvarchar(256) NULL,
        [BuyerRegistrationAddressLine2] nvarchar(256) NULL,
        [BuyerRegistrationCity] nvarchar(128) NULL,
        [BuyerRegistrationPostalCode] nvarchar(32) NULL,
        [BuyerRegistrationCountryCode] char(2) NULL,
        [BuyerRegistrationPrimaryPhone] nvarchar(64) NULL,
        [BuyerRegistrationSecondaryPhone] nvarchar(64) NULL,
        [BuyerRegistrationEmail] nvarchar(320) NULL,
        [EfaImportStatus] [ebay].[orderProcessingStatus] NOT NULL
            CONSTRAINT [DF_EbayOrder_EfaImportStatus] DEFAULT (N'Pending'),
        [EfaOrderReference] nvarchar(128) NULL,
        [EfaImportedAtLocal] [ebay].[fechaLocal] NULL,
        [LastImportBatchId] bigint NOT NULL,
        [FirstImportedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_EbayOrder_FirstImportedAtLocal] DEFAULT (SYSDATETIME()),
        [LastImportedAtLocal] [ebay].[fechaLocal] NOT NULL
            CONSTRAINT [DF_EbayOrder_LastImportedAtLocal] DEFAULT (SYSDATETIME()),
        [RetentionUntilLocal] [ebay].[fechaLocal] NOT NULL,
        CONSTRAINT [PK_EbayOrder] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_EbayOrder_EbayOrderId] UNIQUE ([EbayOrderId]),
        CONSTRAINT [FK_EbayOrder_OrderImportBatch]
            FOREIGN KEY ([LastImportBatchId])
            REFERENCES [ebay].[OrderImportBatch] ([Id]),
        CONSTRAINT [CK_EbayOrder_SellerId]
            CHECK ([SellerId] = N'handwerker3000_de'),
        CONSTRAINT [CK_EbayOrder_EfaImportStatus]
            CHECK ([EfaImportStatus] IN (N'Pending', N'Exported', N'Failed')),
        CONSTRAINT [CK_EbayOrder_ImportedDates]
            CHECK ([LastImportedAtLocal] >= [FirstImportedAtLocal]),
        CONSTRAINT [CK_EbayOrder_Retention]
            CHECK ([RetentionUntilLocal] >= [LastImportedAtLocal]),
        CONSTRAINT [CK_EbayOrder_EfaImportedAt]
            CHECK ([EfaImportedAtLocal] IS NULL OR [EfaImportedAtLocal] >= [FirstImportedAtLocal])
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderShippingAddress]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderShippingAddress]
    (
        [EbayOrderId] bigint NOT NULL,
        [FullName] nvarchar(256) NOT NULL,
        [AddressLine1] nvarchar(256) NOT NULL,
        [AddressLine2] nvarchar(256) NULL,
        [City] nvarchar(128) NOT NULL,
        [StateOrProvince] nvarchar(128) NULL,
        [PostalCode] nvarchar(32) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [PrimaryPhone] nvarchar(64) NULL,
        [Email] nvarchar(320) NULL,
        [ShippingCarrierCode] nvarchar(64) NULL,
        [ShippingServiceCode] nvarchar(128) NULL,
        [MinEstimatedDeliveryDateLocal] [ebay].[fechaLocal] NULL,
        [MaxEstimatedDeliveryDateLocal] [ebay].[fechaLocal] NULL,
        [EbaySupportedFulfillment] bit NULL,
        CONSTRAINT [PK_EbayOrderShippingAddress] PRIMARY KEY CLUSTERED ([EbayOrderId] ASC),
        CONSTRAINT [FK_EbayOrderShippingAddress_EbayOrder]
            FOREIGN KEY ([EbayOrderId])
            REFERENCES [ebay].[EbayOrder] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderShippingAddress_CountryCode]
            CHECK ([CountryCode] = 'DE'),
        CONSTRAINT [CK_EbayOrderShippingAddress_DeliveryDates]
            CHECK ([MaxEstimatedDeliveryDateLocal] IS NULL
                OR [MinEstimatedDeliveryDateLocal] IS NULL
                OR [MaxEstimatedDeliveryDateLocal] >= [MinEstimatedDeliveryDateLocal])
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderLineItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderLineItem]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [EbayOrderId] bigint NOT NULL,
        [EbayLineItemId] [ebay].[eBayLineItemId] NOT NULL,
        [LegacyItemId] [ebay].[eBayItemId] NULL,
        [LegacyVariationId] nvarchar(64) NULL,
        [Sku] [ebay].[SKU] NOT NULL,
        [Title] nvarchar(1024) NOT NULL,
        [Quantity] [ebay].[cantidad] NOT NULL,
        [SoldFormat] nvarchar(32) NULL,
        [ListingMarketplaceId] nvarchar(32) NOT NULL,
        [PurchaseMarketplaceId] nvarchar(32) NOT NULL,
        [LineItemFulfillmentStatus] [ebay].[statusCode] NOT NULL,
        [LineItemCostAmount] [ebay].[dinero] NULL,
        [LineItemCostCurrency] [ebay].[currencyCode] NULL,
        [DiscountedLineItemCostAmount] [ebay].[dinero] NULL,
        [DiscountedLineItemCostCurrency] [ebay].[currencyCode] NULL,
        [TotalAmount] [ebay].[dinero] NOT NULL,
        [TotalCurrency] [ebay].[currencyCode] NOT NULL,
        [ShippingCostAmount] [ebay].[dinero] NULL,
        [ShippingCostCurrency] [ebay].[currencyCode] NULL,
        [ShippingDiscountAmount] [ebay].[dinero] NULL,
        [ShippingDiscountCurrency] [ebay].[currencyCode] NULL,
        [BuyerProtection] bit NULL,
        [MinEstimatedDeliveryDateLocal] [ebay].[fechaLocal] NULL,
        [MaxEstimatedDeliveryDateLocal] [ebay].[fechaLocal] NULL,
        [ShipByDateLocal] [ebay].[fechaLocal] NULL,
        [GuaranteedDelivery] bit NULL,
        [ItemLocation] nvarchar(256) NULL,
        [ItemLocationCountryCode] char(2) NULL,
        [ItemLocationPostalCode] nvarchar(32) NULL,
        CONSTRAINT [PK_EbayOrderLineItem] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_EbayOrderLineItem_Order_LineItem] UNIQUE ([EbayOrderId], [EbayLineItemId]),
        CONSTRAINT [FK_EbayOrderLineItem_EbayOrder]
            FOREIGN KEY ([EbayOrderId])
            REFERENCES [ebay].[EbayOrder] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderLineItem_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_EbayOrderLineItem_Marketplaces]
            CHECK ([ListingMarketplaceId] = N'EBAY_DE' AND [PurchaseMarketplaceId] = N'EBAY_DE'),
        CONSTRAINT [CK_EbayOrderLineItem_DeliveryDates]
            CHECK ([MaxEstimatedDeliveryDateLocal] IS NULL
                OR [MinEstimatedDeliveryDateLocal] IS NULL
                OR [MaxEstimatedDeliveryDateLocal] >= [MinEstimatedDeliveryDateLocal])
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderLineItemVariationAspect]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderLineItemVariationAspect]
    (
        [EbayOrderLineItemId] bigint NOT NULL,
        [SortOrder] int NOT NULL,
        [Name] [ebay].[variationSpecificName] NOT NULL,
        [Value] [ebay].[variationSpecificValue] NOT NULL,
        CONSTRAINT [PK_EbayOrderLineItemVariationAspect]
            PRIMARY KEY CLUSTERED ([EbayOrderLineItemId], [SortOrder]),
        CONSTRAINT [FK_EbayOrderLineItemVariationAspect_EbayOrderLineItem]
            FOREIGN KEY ([EbayOrderLineItemId])
            REFERENCES [ebay].[EbayOrderLineItem] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderLineItemVariationAspect_SortOrder] CHECK ([SortOrder] > 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderLineItemPromotion]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderLineItemPromotion]
    (
        [EbayOrderLineItemId] bigint NOT NULL,
        [SortOrder] int NOT NULL,
        [PromotionId] nvarchar(64) NULL,
        [Description] nvarchar(1024) NULL,
        [DiscountAmount] [ebay].[dinero] NOT NULL,
        [DiscountCurrency] [ebay].[currencyCode] NOT NULL,
        CONSTRAINT [PK_EbayOrderLineItemPromotion]
            PRIMARY KEY CLUSTERED ([EbayOrderLineItemId], [SortOrder]),
        CONSTRAINT [FK_EbayOrderLineItemPromotion_EbayOrderLineItem]
            FOREIGN KEY ([EbayOrderLineItemId])
            REFERENCES [ebay].[EbayOrderLineItem] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderLineItemPromotion_SortOrder] CHECK ([SortOrder] > 0),
        CONSTRAINT [CK_EbayOrderLineItemPromotion_DiscountAmount] CHECK ([DiscountAmount] >= 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderLineItemTax]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderLineItemTax]
    (
        [EbayOrderLineItemId] bigint NOT NULL,
        [TaxKind] nvarchar(32) NOT NULL,
        [SortOrder] int NOT NULL,
        [TaxType] nvarchar(64) NOT NULL,
        [Amount] [ebay].[dinero] NOT NULL,
        [Currency] [ebay].[currencyCode] NOT NULL,
        [CollectionMethod] nvarchar(32) NULL,
        CONSTRAINT [PK_EbayOrderLineItemTax]
            PRIMARY KEY CLUSTERED ([EbayOrderLineItemId], [TaxKind], [SortOrder]),
        CONSTRAINT [FK_EbayOrderLineItemTax_EbayOrderLineItem]
            FOREIGN KEY ([EbayOrderLineItemId])
            REFERENCES [ebay].[EbayOrderLineItem] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderLineItemTax_Kind]
            CHECK ([TaxKind] IN (N'Tax', N'EbayCollectAndRemitTax')),
        CONSTRAINT [CK_EbayOrderLineItemTax_SortOrder] CHECK ([SortOrder] > 0),
        CONSTRAINT [CK_EbayOrderLineItemTax_Amount] CHECK ([Amount] >= 0)
    );
END;
GO

IF OBJECT_ID(N'[ebay].[EbayOrderRefund]', N'U') IS NULL
BEGIN
    CREATE TABLE [ebay].[EbayOrderRefund]
    (
        [Id] bigint IDENTITY(1,1) NOT NULL,
        [EbayOrderId] bigint NOT NULL,
        [EbayRefundId] nvarchar(128) NOT NULL,
        [RefundReferenceId] nvarchar(128) NULL,
        [RefundStatus] [ebay].[statusCode] NOT NULL,
        [RefundDateLocal] [ebay].[fechaLocal] NULL,
        [Amount] [ebay].[dinero] NOT NULL,
        [Currency] [ebay].[currencyCode] NOT NULL,
        CONSTRAINT [PK_EbayOrderRefund] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_EbayOrderRefund_Order_Refund] UNIQUE ([EbayOrderId], [EbayRefundId]),
        CONSTRAINT [FK_EbayOrderRefund_EbayOrder]
            FOREIGN KEY ([EbayOrderId])
            REFERENCES [ebay].[EbayOrder] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_EbayOrderRefund_Amount] CHECK ([Amount] >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OrderImportBatch_SourceContentRetentionUntilLocal' AND [object_id] = OBJECT_ID(N'[ebay].[OrderImportBatch]'))
BEGIN
    CREATE INDEX [IX_OrderImportBatch_SourceContentRetentionUntilLocal]
        ON [ebay].[OrderImportBatch] ([SourceContentRetentionUntilLocal])
        WHERE [SourceContent] IS NOT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_OrderImportBatch_RetentionUntilLocal' AND [object_id] = OBJECT_ID(N'[ebay].[OrderImportBatch]'))
BEGIN
    CREATE INDEX [IX_OrderImportBatch_RetentionUntilLocal]
        ON [ebay].[OrderImportBatch] ([RetentionUntilLocal]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EbayOrder_EfaImportStatus_CreationDateLocal' AND [object_id] = OBJECT_ID(N'[ebay].[EbayOrder]'))
BEGIN
    CREATE INDEX [IX_EbayOrder_EfaImportStatus_CreationDateLocal]
        ON [ebay].[EbayOrder] ([EfaImportStatus], [CreationDateLocal], [Id]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EbayOrder_LastModifiedDateLocal' AND [object_id] = OBJECT_ID(N'[ebay].[EbayOrder]'))
BEGIN
    CREATE INDEX [IX_EbayOrder_LastModifiedDateLocal]
        ON [ebay].[EbayOrder] ([LastModifiedDateLocal], [Id]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EbayOrder_RetentionUntilLocal' AND [object_id] = OBJECT_ID(N'[ebay].[EbayOrder]'))
BEGIN
    CREATE INDEX [IX_EbayOrder_RetentionUntilLocal]
        ON [ebay].[EbayOrder] ([RetentionUntilLocal]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_EbayOrderLineItem_Sku' AND [object_id] = OBJECT_ID(N'[ebay].[EbayOrderLineItem]'))
BEGIN
    CREATE INDEX [IX_EbayOrderLineItem_Sku]
        ON [ebay].[EbayOrderLineItem] ([Sku]);
END;
GO
