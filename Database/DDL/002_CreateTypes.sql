USE [EBAY];
GO

IF TYPE_ID(N'ebay.dinero') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[dinero] FROM decimal(18,4) NULL;');
END;
GO

IF TYPE_ID(N'ebay.cantidad') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[cantidad] FROM int NULL;');
END;
GO

IF TYPE_ID(N'ebay.fechaLocal') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[fechaLocal] FROM datetime2(7) NULL;');
END;
GO

IF TYPE_ID(N'ebay.SKU') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[SKU] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.eBayItemId') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[eBayItemId] FROM nvarchar(32) NULL;');
END;
GO

IF TYPE_ID(N'ebay.taskId') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[taskId] FROM nvarchar(128) NULL;');
END;
GO

IF TYPE_ID(N'ebay.feedType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[feedType] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.sourceName') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[sourceName] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.statusCode') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[statusCode] FROM nvarchar(32) NULL;');
END;
GO

IF TYPE_ID(N'ebay.fileName') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[fileName] FROM nvarchar(260) NULL;');
END;
GO

IF TYPE_ID(N'ebay.sha256') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[sha256] FROM char(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.currencyCode') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[currencyCode] FROM char(3) NULL;');
END;
GO

IF TYPE_ID(N'ebay.efaItem') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[efaItem] FROM nvarchar(47) NULL;');
END;
GO

IF TYPE_ID(N'ebay.efaSalesUnit') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[efaSalesUnit] FROM nvarchar(3) NULL;');
END;
GO

IF TYPE_ID(N'ebay.validationStatus') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[validationStatus] FROM nvarchar(32) NULL;');
END;
GO

IF TYPE_ID(N'ebay.issueType') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[issueType] FROM nvarchar(64) NULL;');
END;
GO

IF TYPE_ID(N'ebay.severity') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[severity] FROM nvarchar(16) NULL;');
END;
GO

IF TYPE_ID(N'ebay.variationSpecificName') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[variationSpecificName] FROM nvarchar(128) NULL;');
END;
GO

IF TYPE_ID(N'ebay.variationSpecificValue') IS NULL
BEGIN
    EXEC(N'CREATE TYPE [ebay].[variationSpecificValue] FROM nvarchar(256) NULL;');
END;
GO
