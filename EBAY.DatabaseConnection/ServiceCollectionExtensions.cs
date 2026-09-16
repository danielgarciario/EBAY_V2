using EBAY.DatabaseConnection.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBAY.DatabaseConnection;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddEBAYDatabaseConnection(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EBAYDB>(config.GetSection(EBAYDB.EBAYDBOptionsKey));
        services.AddSingleton<IEbayDatabaseConnectionFactory, EbayDatabaseConnectionFactory>();
        return services;
    }
}
