USE [EBAY];
GO

IF TYPE_ID(N'ebay.eBayOrderId') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[eBayOrderId] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.eBayLineItemId') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[eBayLineItemId] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.orderImportSource') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[orderImportSource] FROM nvarchar(16) NULL;');
END;
GO

IF TYPE_ID(N'ebay.orderProcessingStatus') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[orderProcessingStatus] FROM nvarchar(16) NULL;');
END;
GO
