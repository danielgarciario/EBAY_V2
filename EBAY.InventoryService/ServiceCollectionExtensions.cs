using EBAY.DatabaseConnection;
using EBAY.InventoryService.InventoryImport;
using EBAY.InventoryService.InventoryUpdate;
using EBAYHttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBAY.InventoryService;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddEbayInventoryService(this IServiceCollection services, IConfiguration config)
    {
        services.AddEbayHttpClient(config);
        services.AddEBAYDatabaseConnection(config);
        services.AddSingleton<InventoryReportParser>();
        services.AddScoped<IInventoryReportImportService, InventoryReportImportService>();
        services.AddScoped<IInventoryUpdateSource, SqlInventoryUpdateSource>();
        services.AddSingleton<InventoryUpdateExecutionLock>();
        services.AddScoped<IInventoryUpdateService, InventoryUpdateService>();
        return services;
    }

}
