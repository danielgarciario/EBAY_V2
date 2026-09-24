USE [EBAY];
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[ebay].[ImportOrders]', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE [ebay].[ImportOrders];
END;
GO

CREATE PROCEDURE [ebay].[ImportOrders]
    @Source [ebay].[orderImportSource],
    @SourceReference nvarchar(2048) = NULL,
    @SourceContent nvarchar(max),
    @SourceContentSha256 [ebay].[sha256],
    @StartedAtLocal [ebay].[fechaLocal] = NULL,
    @CompletedAtLocal [ebay].[fechaLocal] = NULL,
    @ImportedAtLocal [ebay].[fechaLocal] = NULL,
    @Orders [ebay].[OrderImportTableType] READONLY,
    @ShippingAddresses [ebay].[OrderShippingAddressImportTableType] READONLY,
    @LineItems [ebay].[OrderLineItemImportTableType] READONLY,
    @VariationAspects [ebay].[OrderLineItemVariationAspectImportTableType] READONLY,
    @Promotions [ebay].[OrderLineItemPromotionImportTableType] READONLY,
    @Taxes [ebay].[OrderLineItemTaxImportTableType] READONLY,
    @Refunds [ebay].[OrderRefundImportTableType] READONLY,
    @ImportBatchId bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NowLocal datetime2(7) = SYSDATETIME();
    DECLARE @EffectiveImportedAtLocal datetime2(7) = ISNULL(@ImportedAtLocal, @NowLocal);
    DECLARE @EffectiveStartedAtLocal datetime2(7) = ISNULL(@StartedAtLocal, @EffectiveImportedAtLocal);
    DECLARE @EffectiveCompletedAtLocal datetime2(7) = ISNULL(@CompletedAtLocal, @EffectiveImportedAtLocal);
    DECLARE @SourceContentRetentionUntilLocal datetime2(7) = DATEADD(day, 30, @EffectiveImportedAtLocal);
    DECLARE @RetentionUntilLocal datetime2(7) = DATEADD(month, 6, @EffectiveImportedAtLocal);
    DECLARE @OrderCount int = (SELECT COUNT(1) FROM @Orders);

    IF @Source NOT IN (N'Polling', N'Webhook')
    BEGIN
        THROW 51300, 'El origen de importación debe ser Polling o Webhook.', 1;
    END;

    IF @SourceContent IS NULL OR ISJSON(@SourceContent) <> 1
    BEGIN
        THROW 51301, 'El contenido original debe contener un documento JSON válido.', 1;
    END;

    IF @SourceContentSha256 IS NULL OR LEN(@SourceContentSha256) <> 64
    BEGIN
        THROW 51302, 'El hash SHA-256 del contenido original debe tener 64 caracteres.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Orders AS o
        WHERE o.[SellerId] <> N'handwerker3000_de'
    )
    BEGIN
        THROW 51303, 'Todos los pedidos deben pertenecer al vendedor handwerker3000_de.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @ShippingAddresses AS s
        LEFT JOIN @Orders AS o
            ON o.[OrderRowNumber] = s.[OrderRowNumber]
        WHERE o.[OrderRowNumber] IS NULL
           OR s.[CountryCode] <> 'DE'
    )
    BEGIN
        THROW 51304, 'Las direcciones de envío deben pertenecer a pedidos importados y ser de Alemania.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Orders AS o
        LEFT JOIN @ShippingAddresses AS s
            ON s.[OrderRowNumber] = o.[OrderRowNumber]
        WHERE s.[OrderRowNumber] IS NULL
    )
    BEGIN
        THROW 51305, 'Cada pedido debe incluir exactamente una dirección de envío SHIP_TO.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @LineItems AS l
        LEFT JOIN @Orders AS o
            ON o.[OrderRowNumber] = l.[OrderRowNumber]
        WHERE o.[OrderRowNumber] IS NULL
           OR l.[ListingMarketplaceId] <> N'EBAY_DE'
           OR l.[PurchaseMarketplaceId] <> N'EBAY_DE'
    )
    BEGIN
        THROW 51306, 'Las líneas deben pertenecer a pedidos importados del mercado EBAY_DE.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Orders AS o
        LEFT JOIN @LineItems AS l
            ON l.[OrderRowNumber] = o.[OrderRowNumber]
        WHERE l.[OrderRowNumber] IS NULL
    )
    BEGIN
        THROW 51307, 'Cada pedido debe incluir al menos una línea.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @VariationAspects AS a
        LEFT JOIN @LineItems AS l
            ON l.[LineItemRowNumber] = a.[LineItemRowNumber]
        WHERE l.[LineItemRowNumber] IS NULL
    )
    BEGIN
        THROW 51308, 'Las características de variación deben pertenecer a líneas importadas.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Promotions AS p
        LEFT JOIN @LineItems AS l
            ON l.[LineItemRowNumber] = p.[LineItemRowNumber]
        WHERE l.[LineItemRowNumber] IS NULL
    )
    BEGIN
        THROW 51309, 'Las promociones deben pertenecer a líneas importadas.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Taxes AS t
        LEFT JOIN @LineItems AS l
            ON l.[LineItemRowNumber] = t.[LineItemRowNumber]
        WHERE l.[LineItemRowNumber] IS NULL
           OR t.[TaxKind] NOT IN (N'Tax', N'EbayCollectAndRemitTax')
    )
    BEGIN
        THROW 51310, 'Los impuestos deben pertenecer a líneas importadas y usar un tipo permitido.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Refunds AS r
        LEFT JOIN @Orders AS o
            ON o.[OrderRowNumber] = r.[OrderRowNumber]
        WHERE o.[OrderRowNumber] IS NULL
    )
    BEGIN
        THROW 51311, 'Los reembolsos deben pertenecer a pedidos importados.', 1;
    END;

    DECLARE @AcceptedOrders TABLE
    (
        [OrderRowNumber] int NOT NULL PRIMARY KEY
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [ebay].[OrderImportBatch]
        (
            [Source],
            [SourceReference],
            [SourceContent],
            [SourceContentSha256],
            [StartedAtLocal],
            [CompletedAtLocal],
            [ImportedAtLocal],
            [SourceContentRetentionUntilLocal],
            [RetentionUntilLocal],
            [OrderCount]
        )
        VALUES
        (
            @Source,
            NULLIF(LTRIM(RTRIM(@SourceReference)), N''),
            @SourceContent,
            @SourceContentSha256,
            @EffectiveStartedAtLocal,
            @EffectiveCompletedAtLocal,
            @EffectiveImportedAtLocal,
            @SourceContentRetentionUntilLocal,
            @RetentionUntilLocal,
            @OrderCount
        );

        SET @ImportBatchId = CONVERT(bigint, SCOPE_IDENTITY());

        INSERT INTO @AcceptedOrders ([OrderRowNumber])
        SELECT o.[OrderRowNumber]
        FROM @Orders AS o
        LEFT JOIN [ebay].[EbayOrder] AS target WITH (UPDLOCK, HOLDLOCK)
            ON target.[EbayOrderId] = o.[EbayOrderId]
        WHERE target.[Id] IS NULL
           OR o.[LastModifiedDateLocal] >= target.[LastModifiedDateLocal];

        MERGE [ebay].[EbayOrder] WITH (HOLDLOCK) AS target
        USING
        (
            SELECT o.*
            FROM @Orders AS o
            INNER JOIN @AcceptedOrders AS accepted
                ON accepted.[OrderRowNumber] = o.[OrderRowNumber]
        ) AS source
            ON target.[EbayOrderId] = source.[EbayOrderId]
        WHEN MATCHED THEN
            UPDATE SET
                [LegacyOrderId] = source.[LegacyOrderId],
                [SalesRecordReference] = source.[SalesRecordReference],
                [SellerId] = source.[SellerId],
                [BuyerUsername] = source.[BuyerUsername],
                [CreationDateLocal] = source.[CreationDateLocal],
                [LastModifiedDateLocal] = source.[LastModifiedDateLocal],
                [OrderFulfillmentStatus] = source.[OrderFulfillmentStatus],
                [OrderPaymentStatus] = source.[OrderPaymentStatus],
                [CancelState] = source.[CancelState],
                [PriceSubtotalAmount] = source.[PriceSubtotalAmount],
                [PriceSubtotalCurrency] = source.[PriceSubtotalCurrency],
                [PriceDiscountAmount] = source.[PriceDiscountAmount],
                [DeliveryCostAmount] = source.[DeliveryCostAmount],
                [DeliveryDiscountAmount] = source.[DeliveryDiscountAmount],
                [TotalAmount] = source.[TotalAmount],
                [TotalCurrency] = source.[TotalCurrency],
                [TotalFeeBasisAmount] = source.[TotalFeeBasisAmount],
                [TotalFeeBasisCurrency] = source.[TotalFeeBasisCurrency],
                [TotalMarketplaceFeeAmount] = source.[TotalMarketplaceFeeAmount],
                [TotalMarketplaceFeeCurrency] = source.[TotalMarketplaceFeeCurrency],
                [BuyerTaxCity] = source.[BuyerTaxCity],
                [BuyerTaxPostalCode] = source.[BuyerTaxPostalCode],
                [BuyerTaxCountryCode] = source.[BuyerTaxCountryCode],
                [BuyerRegistrationFullName] = source.[BuyerRegistrationFullName],
                [BuyerRegistrationAddressLine1] = source.[BuyerRegistrationAddressLine1],
                [BuyerRegistrationAddressLine2] = source.[BuyerRegistrationAddressLine2],
                [BuyerRegistrationCity] = source.[BuyerRegistrationCity],
                [BuyerRegistrationPostalCode] = source.[BuyerRegistrationPostalCode],
                [BuyerRegistrationCountryCode] = source.[BuyerRegistrationCountryCode],
                [BuyerRegistrationPrimaryPhone] = source.[BuyerRegistrationPrimaryPhone],
                [BuyerRegistrationSecondaryPhone] = source.[BuyerRegistrationSecondaryPhone],
                [BuyerRegistrationEmail] = source.[BuyerRegistrationEmail],
                [LastImportBatchId] = @ImportBatchId,
                [LastImportedAtLocal] = @EffectiveImportedAtLocal,
                [RetentionUntilLocal] = @RetentionUntilLocal
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [EbayOrderId], [LegacyOrderId], [SalesRecordReference], [SellerId], [BuyerUsername],
                [CreationDateLocal], [LastModifiedDateLocal], [OrderFulfillmentStatus], [OrderPaymentStatus], [CancelState],
                [PriceSubtotalAmount], [PriceSubtotalCurrency], [PriceDiscountAmount], [DeliveryCostAmount], [DeliveryDiscountAmount],
                [TotalAmount], [TotalCurrency], [TotalFeeBasisAmount], [TotalFeeBasisCurrency],
                [TotalMarketplaceFeeAmount], [TotalMarketplaceFeeCurrency],
                [BuyerTaxCity], [BuyerTaxPostalCode], [BuyerTaxCountryCode],
                [BuyerRegistrationFullName], [BuyerRegistrationAddressLine1], [BuyerRegistrationAddressLine2],
                [BuyerRegistrationCity], [BuyerRegistrationPostalCode], [BuyerRegistrationCountryCode],
                [BuyerRegistrationPrimaryPhone], [BuyerRegistrationSecondaryPhone], [BuyerRegistrationEmail],
                [LastImportBatchId], [FirstImportedAtLocal], [LastImportedAtLocal], [RetentionUntilLocal]
            )
            VALUES
            (
                source.[EbayOrderId], source.[LegacyOrderId], source.[SalesRecordReference], source.[SellerId], source.[BuyerUsername],
                source.[CreationDateLocal], source.[LastModifiedDateLocal], source.[OrderFulfillmentStatus], source.[OrderPaymentStatus], source.[CancelState],
                source.[PriceSubtotalAmount], source.[PriceSubtotalCurrency], source.[PriceDiscountAmount], source.[DeliveryCostAmount], source.[DeliveryDiscountAmount],
                source.[TotalAmount], source.[TotalCurrency], source.[TotalFeeBasisAmount], source.[TotalFeeBasisCurrency],
                source.[TotalMarketplaceFeeAmount], source.[TotalMarketplaceFeeCurrency],
                source.[BuyerTaxCity], source.[BuyerTaxPostalCode], source.[BuyerTaxCountryCode],
                source.[BuyerRegistrationFullName], source.[BuyerRegistrationAddressLine1], source.[BuyerRegistrationAddressLine2],
                source.[BuyerRegistrationCity], source.[BuyerRegistrationPostalCode], source.[BuyerRegistrationCountryCode],
                source.[BuyerRegistrationPrimaryPhone], source.[BuyerRegistrationSecondaryPhone], source.[BuyerRegistrationEmail],
                @ImportBatchId, @EffectiveImportedAtLocal, @EffectiveImportedAtLocal, @RetentionUntilLocal
            );

        DECLARE @OrderMap TABLE
        (
            [OrderRowNumber] int NOT NULL PRIMARY KEY,
            [EbayOrderInternalId] bigint NOT NULL
        );

        INSERT INTO @OrderMap ([OrderRowNumber], [EbayOrderInternalId])
        SELECT source.[OrderRowNumber], target.[Id]
        FROM @Orders AS source
        INNER JOIN @AcceptedOrders AS accepted
            ON accepted.[OrderRowNumber] = source.[OrderRowNumber]
        INNER JOIN [ebay].[EbayOrder] AS target
            ON target.[EbayOrderId] = source.[EbayOrderId];

        DELETE target
        FROM [ebay].[EbayOrderShippingAddress] AS target
        INNER JOIN @OrderMap AS map
            ON map.[EbayOrderInternalId] = target.[EbayOrderId]
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM @ShippingAddresses AS source
            WHERE source.[OrderRowNumber] = map.[OrderRowNumber]
        );

        MERGE [ebay].[EbayOrderShippingAddress] AS target
        USING
        (
            SELECT map.[EbayOrderInternalId], source.*
            FROM @ShippingAddresses AS source
            INNER JOIN @OrderMap AS map
                ON map.[OrderRowNumber] = source.[OrderRowNumber]
        ) AS source
            ON target.[EbayOrderId] = source.[EbayOrderInternalId]
        WHEN MATCHED THEN
            UPDATE SET
                [FullName] = source.[FullName], [AddressLine1] = source.[AddressLine1], [AddressLine2] = source.[AddressLine2],
                [City] = source.[City], [StateOrProvince] = source.[StateOrProvince], [PostalCode] = source.[PostalCode],
                [CountryCode] = source.[CountryCode], [PrimaryPhone] = source.[PrimaryPhone], [Email] = source.[Email],
                [ShippingCarrierCode] = source.[ShippingCarrierCode], [ShippingServiceCode] = source.[ShippingServiceCode],
                [MinEstimatedDeliveryDateLocal] = source.[MinEstimatedDeliveryDateLocal],
                [MaxEstimatedDeliveryDateLocal] = source.[MaxEstimatedDeliveryDateLocal],
                [EbaySupportedFulfillment] = source.[EbaySupportedFulfillment]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [EbayOrderId], [FullName], [AddressLine1], [AddressLine2], [City], [StateOrProvince], [PostalCode], [CountryCode],
                [PrimaryPhone], [Email], [ShippingCarrierCode], [ShippingServiceCode], [MinEstimatedDeliveryDateLocal],
                [MaxEstimatedDeliveryDateLocal], [EbaySupportedFulfillment]
            )
            VALUES
            (
                source.[EbayOrderInternalId], source.[FullName], source.[AddressLine1], source.[AddressLine2], source.[City],
                source.[StateOrProvince], source.[PostalCode], source.[CountryCode], source.[PrimaryPhone], source.[Email],
                source.[ShippingCarrierCode], source.[ShippingServiceCode], source.[MinEstimatedDeliveryDateLocal],
                source.[MaxEstimatedDeliveryDateLocal], source.[EbaySupportedFulfillment]
            );

        DELETE target
        FROM [ebay].[EbayOrderLineItem] AS target
        INNER JOIN @OrderMap AS map
            ON map.[EbayOrderInternalId] = target.[EbayOrderId]
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM @LineItems AS source
            WHERE source.[OrderRowNumber] = map.[OrderRowNumber]
              AND source.[EbayLineItemId] = target.[EbayLineItemId]
        );

        MERGE [ebay].[EbayOrderLineItem] AS target
        USING
        (
            SELECT map.[EbayOrderInternalId], source.*
            FROM @LineItems AS source
            INNER JOIN @OrderMap AS map
                ON map.[OrderRowNumber] = source.[OrderRowNumber]
        ) AS source
            ON target.[EbayOrderId] = source.[EbayOrderInternalId]
           AND target.[EbayLineItemId] = source.[EbayLineItemId]
        WHEN MATCHED THEN
            UPDATE SET
                [LegacyItemId] = source.[LegacyItemId], [LegacyVariationId] = source.[LegacyVariationId], [Sku] = source.[Sku],
                [Title] = source.[Title], [Quantity] = source.[Quantity], [SoldFormat] = source.[SoldFormat],
                [ListingMarketplaceId] = source.[ListingMarketplaceId], [PurchaseMarketplaceId] = source.[PurchaseMarketplaceId],
                [LineItemFulfillmentStatus] = source.[LineItemFulfillmentStatus], [LineItemCostAmount] = source.[LineItemCostAmount],
                [LineItemCostCurrency] = source.[LineItemCostCurrency], [DiscountedLineItemCostAmount] = source.[DiscountedLineItemCostAmount],
                [DiscountedLineItemCostCurrency] = source.[DiscountedLineItemCostCurrency], [TotalAmount] = source.[TotalAmount],
                [TotalCurrency] = source.[TotalCurrency], [ShippingCostAmount] = source.[ShippingCostAmount],
                [ShippingCostCurrency] = source.[ShippingCostCurrency], [ShippingDiscountAmount] = source.[ShippingDiscountAmount],
                [ShippingDiscountCurrency] = source.[ShippingDiscountCurrency], [BuyerProtection] = source.[BuyerProtection],
                [MinEstimatedDeliveryDateLocal] = source.[MinEstimatedDeliveryDateLocal],
                [MaxEstimatedDeliveryDateLocal] = source.[MaxEstimatedDeliveryDateLocal], [ShipByDateLocal] = source.[ShipByDateLocal],
                [GuaranteedDelivery] = source.[GuaranteedDelivery], [ItemLocation] = source.[ItemLocation],
                [ItemLocationCountryCode] = source.[ItemLocationCountryCode], [ItemLocationPostalCode] = source.[ItemLocationPostalCode]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT
            (
                [EbayOrderId], [EbayLineItemId], [LegacyItemId], [LegacyVariationId], [Sku], [Title], [Quantity], [SoldFormat],
                [ListingMarketplaceId], [PurchaseMarketplaceId], [LineItemFulfillmentStatus], [LineItemCostAmount], [LineItemCostCurrency],
                [DiscountedLineItemCostAmount], [DiscountedLineItemCostCurrency], [TotalAmount], [TotalCurrency], [ShippingCostAmount],
                [ShippingCostCurrency], [ShippingDiscountAmount], [ShippingDiscountCurrency], [BuyerProtection],
                [MinEstimatedDeliveryDateLocal], [MaxEstimatedDeliveryDateLocal], [ShipByDateLocal], [GuaranteedDelivery],
                [ItemLocation], [ItemLocationCountryCode], [ItemLocationPostalCode]
            )
            VALUES
            (
                source.[EbayOrderInternalId], source.[EbayLineItemId], source.[LegacyItemId], source.[LegacyVariationId], source.[Sku],
                source.[Title], source.[Quantity], source.[SoldFormat], source.[ListingMarketplaceId], source.[PurchaseMarketplaceId],
                source.[LineItemFulfillmentStatus], source.[LineItemCostAmount], source.[LineItemCostCurrency],
                source.[DiscountedLineItemCostAmount], source.[DiscountedLineItemCostCurrency], source.[TotalAmount], source.[TotalCurrency],
                source.[ShippingCostAmount], source.[ShippingCostCurrency], source.[ShippingDiscountAmount], source.[ShippingDiscountCurrency],
                source.[BuyerProtection], source.[MinEstimatedDeliveryDateLocal], source.[MaxEstimatedDeliveryDateLocal], source.[ShipByDateLocal],
                source.[GuaranteedDelivery], source.[ItemLocation], source.[ItemLocationCountryCode], source.[ItemLocationPostalCode]
            );

        DECLARE @LineItemMap TABLE
        (
            [LineItemRowNumber] int NOT NULL PRIMARY KEY,
            [EbayOrderLineItemInternalId] bigint NOT NULL
        );

        INSERT INTO @LineItemMap ([LineItemRowNumber], [EbayOrderLineItemInternalId])
        SELECT source.[LineItemRowNumber], target.[Id]
        FROM @LineItems AS source
        INNER JOIN @OrderMap AS map
            ON map.[OrderRowNumber] = source.[OrderRowNumber]
        INNER JOIN [ebay].[EbayOrderLineItem] AS target
            ON target.[EbayOrderId] = map.[EbayOrderInternalId]
           AND target.[EbayLineItemId] = source.[EbayLineItemId];

        DELETE target
        FROM [ebay].[EbayOrderLineItemVariationAspect] AS target
        INNER JOIN @LineItemMap AS map
            ON map.[EbayOrderLineItemInternalId] = target.[EbayOrderLineItemId]
        WHERE NOT EXISTS
        (
            SELECT 1 FROM @VariationAspects AS source
            WHERE source.[LineItemRowNumber] = map.[LineItemRowNumber]
              AND source.[SortOrder] = target.[SortOrder]
        );

        MERGE [ebay].[EbayOrderLineItemVariationAspect] AS target
        USING
        (
            SELECT map.[EbayOrderLineItemInternalId], source.*
            FROM @VariationAspects AS source
            INNER JOIN @LineItemMap AS map
                ON map.[LineItemRowNumber] = source.[LineItemRowNumber]
        ) AS source
            ON target.[EbayOrderLineItemId] = source.[EbayOrderLineItemInternalId]
           AND target.[SortOrder] = source.[SortOrder]
        WHEN MATCHED THEN UPDATE SET [Name] = source.[Name], [Value] = source.[Value]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([EbayOrderLineItemId], [SortOrder], [Name], [Value])
            VALUES (source.[EbayOrderLineItemInternalId], source.[SortOrder], source.[Name], source.[Value]);

        DELETE target
        FROM [ebay].[EbayOrderLineItemPromotion] AS target
        INNER JOIN @LineItemMap AS map
            ON map.[EbayOrderLineItemInternalId] = target.[EbayOrderLineItemId]
        WHERE NOT EXISTS
        (
            SELECT 1 FROM @Promotions AS source
            WHERE source.[LineItemRowNumber] = map.[LineItemRowNumber]
              AND source.[SortOrder] = target.[SortOrder]
        );

        MERGE [ebay].[EbayOrderLineItemPromotion] AS target
        USING
        (
            SELECT map.[EbayOrderLineItemInternalId], source.*
            FROM @Promotions AS source
            INNER JOIN @LineItemMap AS map
                ON map.[LineItemRowNumber] = source.[LineItemRowNumber]
        ) AS source
            ON target.[EbayOrderLineItemId] = source.[EbayOrderLineItemInternalId]
           AND target.[SortOrder] = source.[SortOrder]
        WHEN MATCHED THEN
            UPDATE SET [PromotionId] = source.[PromotionId], [Description] = source.[Description],
                [DiscountAmount] = source.[DiscountAmount], [DiscountCurrency] = source.[DiscountCurrency]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([EbayOrderLineItemId], [SortOrder], [PromotionId], [Description], [DiscountAmount], [DiscountCurrency])
            VALUES (source.[EbayOrderLineItemInternalId], source.[SortOrder], source.[PromotionId], source.[Description], source.[DiscountAmount], source.[DiscountCurrency]);

        DELETE target
        FROM [ebay].[EbayOrderLineItemTax] AS target
        INNER JOIN @LineItemMap AS map
            ON map.[EbayOrderLineItemInternalId] = target.[EbayOrderLineItemId]
        WHERE NOT EXISTS
        (
            SELECT 1 FROM @Taxes AS source
            WHERE source.[LineItemRowNumber] = map.[LineItemRowNumber]
              AND source.[TaxKind] = target.[TaxKind]
              AND source.[SortOrder] = target.[SortOrder]
        );

        MERGE [ebay].[EbayOrderLineItemTax] AS target
        USING
        (
            SELECT map.[EbayOrderLineItemInternalId], source.*
            FROM @Taxes AS source
            INNER JOIN @LineItemMap AS map
                ON map.[LineItemRowNumber] = source.[LineItemRowNumber]
        ) AS source
            ON target.[EbayOrderLineItemId] = source.[EbayOrderLineItemInternalId]
           AND target.[TaxKind] = source.[TaxKind]
           AND target.[SortOrder] = source.[SortOrder]
        WHEN MATCHED THEN
            UPDATE SET [TaxType] = source.[TaxType], [Amount] = source.[Amount], [Currency] = source.[Currency],
                [CollectionMethod] = source.[CollectionMethod]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([EbayOrderLineItemId], [TaxKind], [SortOrder], [TaxType], [Amount], [Currency], [CollectionMethod])
            VALUES (source.[EbayOrderLineItemInternalId], source.[TaxKind], source.[SortOrder], source.[TaxType], source.[Amount], source.[Currency], source.[CollectionMethod]);

        DELETE target
        FROM [ebay].[EbayOrderRefund] AS target
        INNER JOIN @OrderMap AS map
            ON map.[EbayOrderInternalId] = target.[EbayOrderId]
        WHERE NOT EXISTS
        (
            SELECT 1 FROM @Refunds AS source
            WHERE source.[OrderRowNumber] = map.[OrderRowNumber]
              AND source.[EbayRefundId] = target.[EbayRefundId]
        );

        MERGE [ebay].[EbayOrderRefund] AS target
        USING
        (
            SELECT map.[EbayOrderInternalId], source.*
            FROM @Refunds AS source
            INNER JOIN @OrderMap AS map
                ON map.[OrderRowNumber] = source.[OrderRowNumber]
        ) AS source
            ON target.[EbayOrderId] = source.[EbayOrderInternalId]
           AND target.[EbayRefundId] = source.[EbayRefundId]
        WHEN MATCHED THEN
            UPDATE SET [RefundReferenceId] = source.[RefundReferenceId], [RefundStatus] = source.[RefundStatus],
                [RefundDateLocal] = source.[RefundDateLocal], [Amount] = source.[Amount], [Currency] = source.[Currency]
        WHEN NOT MATCHED BY TARGET THEN
            INSERT ([EbayOrderId], [EbayRefundId], [RefundReferenceId], [RefundStatus], [RefundDateLocal], [Amount], [Currency])
            VALUES (source.[EbayOrderInternalId], source.[EbayRefundId], source.[RefundReferenceId], source.[RefundStatus], source.[RefundDateLocal], source.[Amount], source.[Currency]);

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
        @OrderCount AS [ReceivedOrderCount],
        (SELECT COUNT(1) FROM @AcceptedOrders) AS [ImportedOrUpdatedOrderCount];
END;
GO
