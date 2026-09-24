using EBAY.OrdersService.OrderImport;
using EBAY.DatabaseConnection;
using EBAYHttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBAY.OrdersService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEbayOrdersService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEbayHttpClient(configuration);
        services.AddEBAYDatabaseConnection(configuration);
        services.AddOptions<OrderImportOptions>()
            .Bind(configuration.GetSection(OrderImportOptions.SectionName))
            .Validate(
                options => options.PollingLookbackMinutes > 0 && options.PollingLookbackMinutes <= 1440,
                "Das Abrufintervall für eBay-Bestellungen muss zwischen 1 und 1440 Minuten liegen.")
            .Validate(
                options => options.PageSize is > 0 and <= 200,
                "Die Seitengröße für eBay-Bestellungen muss zwischen 1 und 200 liegen.")
            .ValidateOnStart();

        services.AddSingleton<OrderImportExecutionLock>();
        services.AddScoped<IOrderImportService, OrderImportService>();
        return services;
    }
}
