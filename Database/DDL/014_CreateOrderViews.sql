USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[vwLatestOrderImportBatch]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwLatestOrderImportBatch];
END;
GO

IF OBJECT_ID(N'[ebay].[vwEbayOrderOverview]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwEbayOrderOverview];
END;
GO

IF OBJECT_ID(N'[ebay].[vwEbayOrdersReadyForEfa]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwEbayOrdersReadyForEfa];
END;
GO

IF OBJECT_ID(N'[ebay].[vwEbayOrderLineItemDetail]', N'V') IS NOT NULL
BEGIN
    DROP VIEW [ebay].[vwEbayOrderLineItemDetail];
END;
GO

CREATE VIEW [ebay].[vwLatestOrderImportBatch]
AS
    SELECT TOP (1)
        [Id],
        [Source],
        [SourceReference],
        [SourceContentSha256],
        [StartedAtLocal],
        [CompletedAtLocal],
        [ImportedAtLocal],
        [SourceContentRetentionUntilLocal],
        [RetentionUntilLocal],
        [OrderCount],
        CASE WHEN [SourceContent] IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS [HasSourceContent]
    FROM [ebay].[OrderImportBatch]
    ORDER BY [ImportedAtLocal] DESC, [Id] DESC;
GO

CREATE VIEW [ebay].[vwEbayOrderOverview]
AS
    SELECT
        o.[Id] AS [EbayOrderInternalId],
        o.[EbayOrderId],
        o.[LegacyOrderId],
        o.[SalesRecordReference],
        o.[SellerId],
        o.[BuyerUsername],
        o.[CreationDateLocal],
        o.[LastModifiedDateLocal],
        o.[OrderFulfillmentStatus],
        o.[OrderPaymentStatus],
        o.[CancelState],
        o.[PriceSubtotalAmount],
        o.[PriceDiscountAmount],
        o.[DeliveryCostAmount],
        o.[DeliveryDiscountAmount],
        o.[TotalAmount],
        o.[TotalCurrency],
        o.[TotalFeeBasisAmount],
        o.[TotalMarketplaceFeeAmount],
        o.[EfaImportStatus],
        o.[EfaOrderReference],
        o.[EfaImportedAtLocal],
        o.[FirstImportedAtLocal],
        o.[LastImportedAtLocal],
        s.[FullName] AS [ShippingFullName],
        s.[AddressLine1] AS [ShippingAddressLine1],
        s.[AddressLine2] AS [ShippingAddressLine2],
        s.[City] AS [ShippingCity],
        s.[PostalCode] AS [ShippingPostalCode],
        s.[CountryCode] AS [ShippingCountryCode],
        s.[PrimaryPhone] AS [ShippingPrimaryPhone],
        s.[Email] AS [ShippingEmail],
        s.[ShippingCarrierCode],
        s.[ShippingServiceCode],
        s.[MinEstimatedDeliveryDateLocal],
        s.[MaxEstimatedDeliveryDateLocal],
        b.[Source] AS [LastImportSource],
        b.[ImportedAtLocal] AS [LastBatchImportedAtLocal]
    FROM [ebay].[EbayOrder] AS o
    LEFT JOIN [ebay].[EbayOrderShippingAddress] AS s
        ON s.[EbayOrderId] = o.[Id]
    INNER JOIN [ebay].[OrderImportBatch] AS b
        ON b.[Id] = o.[LastImportBatchId];
GO

CREATE VIEW [ebay].[vwEbayOrdersReadyForEfa]
AS
    SELECT
        o.[Id] AS [EbayOrderInternalId],
        o.[EbayOrderId],
        o.[SalesRecordReference],
        o.[CreationDateLocal],
        o.[LastModifiedDateLocal],
        o.[BuyerUsername],
        s.[FullName] AS [ShippingFullName],
        s.[AddressLine1] AS [ShippingAddressLine1],
        s.[AddressLine2] AS [ShippingAddressLine2],
        s.[City] AS [ShippingCity],
        s.[PostalCode] AS [ShippingPostalCode],
        s.[CountryCode] AS [ShippingCountryCode],
        s.[PrimaryPhone] AS [ShippingPrimaryPhone],
        s.[Email] AS [ShippingEmail],
        o.[TotalAmount],
        o.[TotalCurrency],
        o.[EfaImportStatus]
    FROM [ebay].[EbayOrder] AS o
    INNER JOIN [ebay].[EbayOrderShippingAddress] AS s
        ON s.[EbayOrderId] = o.[Id]
    WHERE o.[OrderPaymentStatus] = N'PAID'
      AND o.[CancelState] = N'NONE_REQUESTED'
      AND o.[OrderFulfillmentStatus] IN (N'NOT_STARTED', N'IN_PROGRESS')
      AND o.[EfaImportStatus] = N'Pending'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [ebay].[EbayOrderLineItem] AS l
          WHERE l.[EbayOrderId] = o.[Id]
            AND (CHARINDEX(N'.', l.[Sku]) <= 1 OR RIGHT(l.[Sku], 1) = N'.')
      );
GO

CREATE VIEW [ebay].[vwEbayOrderLineItemDetail]
AS
    SELECT
        o.[EbayOrderId],
        o.[CreationDateLocal],
        o.[OrderPaymentStatus],
        o.[CancelState],
        l.[Id] AS [EbayOrderLineItemInternalId],
        l.[EbayLineItemId],
        l.[LegacyItemId],
        l.[LegacyVariationId],
        l.[Sku],
        CASE
            WHEN CHARINDEX(N'.', REVERSE(l.[Sku])) > 1
                THEN LEFT(l.[Sku], LEN(l.[Sku]) - CHARINDEX(N'.', REVERSE(l.[Sku])))
        END AS [ParsedEfaItem],
        CASE
            WHEN CHARINDEX(N'.', REVERSE(l.[Sku])) > 1
                THEN RIGHT(l.[Sku], CHARINDEX(N'.', REVERSE(l.[Sku])) - 1)
        END AS [ParsedEfaSalesUnit],
        l.[Title],
        l.[Quantity],
        l.[LineItemFulfillmentStatus],
        l.[LineItemCostAmount],
        l.[DiscountedLineItemCostAmount],
        l.[ShippingCostAmount],
        l.[ShippingDiscountAmount],
        l.[TotalAmount],
        l.[TotalCurrency],
        l.[ShipByDateLocal],
        l.[GuaranteedDelivery]
    FROM [ebay].[EbayOrder] AS o
    INNER JOIN [ebay].[EbayOrderLineItem] AS l
        ON l.[EbayOrderId] = o.[Id];
GO
