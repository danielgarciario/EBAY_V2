using EBAYHttpClient.InventoryImport;
using EBAYHttpClient.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBAYHttpClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEbayHttpClient(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EBAYClientOptions>(config.GetSection(EBAYClientOptions.EbayOptionsKey));
        services.Configure<EBAYDB>(config.GetSection(EBAYDB.EBAYDBOptionsKey));

        services.AddSingleton<InventoryReportParser>();
        services.AddScoped<IInventoryReportImportService, InventoryReportImportService>();

        return services;
    }
}
