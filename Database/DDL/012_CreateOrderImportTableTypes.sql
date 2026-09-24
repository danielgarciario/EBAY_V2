USE [EBAY];
GO

IF TYPE_ID(N'ebay.OrderImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderImportTableType] AS TABLE
    (
        [OrderRowNumber] int NOT NULL,
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
        PRIMARY KEY CLUSTERED ([OrderRowNumber]),
        UNIQUE ([EbayOrderId])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderShippingAddressImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderShippingAddressImportTableType] AS TABLE
    (
        [OrderRowNumber] int NOT NULL,
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
        PRIMARY KEY CLUSTERED ([OrderRowNumber])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderLineItemImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderLineItemImportTableType] AS TABLE
    (
        [LineItemRowNumber] int NOT NULL,
        [OrderRowNumber] int NOT NULL,
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
        PRIMARY KEY CLUSTERED ([LineItemRowNumber]),
        UNIQUE ([OrderRowNumber], [EbayLineItemId])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderLineItemVariationAspectImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderLineItemVariationAspectImportTableType] AS TABLE
    (
        [LineItemRowNumber] int NOT NULL,
        [SortOrder] int NOT NULL,
        [Name] [ebay].[variationSpecificName] NOT NULL,
        [Value] [ebay].[variationSpecificValue] NOT NULL,
        PRIMARY KEY CLUSTERED ([LineItemRowNumber], [SortOrder])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderLineItemPromotionImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderLineItemPromotionImportTableType] AS TABLE
    (
        [LineItemRowNumber] int NOT NULL,
        [SortOrder] int NOT NULL,
        [PromotionId] nvarchar(64) NULL,
        [Description] nvarchar(1024) NULL,
        [DiscountAmount] [ebay].[dinero] NOT NULL,
        [DiscountCurrency] [ebay].[currencyCode] NOT NULL,
        PRIMARY KEY CLUSTERED ([LineItemRowNumber], [SortOrder])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderLineItemTaxImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderLineItemTaxImportTableType] AS TABLE
    (
        [LineItemRowNumber] int NOT NULL,
        [TaxKind] nvarchar(32) NOT NULL,
        [SortOrder] int NOT NULL,
        [TaxType] nvarchar(64) NOT NULL,
        [Amount] [ebay].[dinero] NOT NULL,
        [Currency] [ebay].[currencyCode] NOT NULL,
        [CollectionMethod] nvarchar(32) NULL,
        PRIMARY KEY CLUSTERED ([LineItemRowNumber], [TaxKind], [SortOrder])
    );');
END;
GO

IF TYPE_ID(N'ebay.OrderRefundImportTableType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[OrderRefundImportTableType] AS TABLE
    (
        [OrderRowNumber] int NOT NULL,
        [EbayRefundId] nvarchar(128) NOT NULL,
        [RefundReferenceId] nvarchar(128) NULL,
        [RefundStatus] [ebay].[statusCode] NOT NULL,
        [RefundDateLocal] [ebay].[fechaLocal] NULL,
        [Amount] [ebay].[dinero] NOT NULL,
        [Currency] [ebay].[currencyCode] NOT NULL,
        PRIMARY KEY CLUSTERED ([OrderRowNumber], [EbayRefundId])
    );');
END;
GO
