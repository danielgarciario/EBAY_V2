using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EBAY.TRADING.API.CLIENT;

public static class ServiceCollectionExtensions
{


    public static IServiceCollection AddEbayTradingAPI(this IServiceCollection services, IConfiguration config)
    {

        services.Configure<Options.EBAYTradingOptions>(config.GetSection(Options.EBAYTradingOptions.EBAYTradingOptionsKey));

        services.AddSingleton<EBayTradingApiContext>();
        services.AddScoped<EbayTradingService>();
        return services;
    }
}
