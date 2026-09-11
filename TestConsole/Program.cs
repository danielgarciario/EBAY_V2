

using EBAY.TRADING.API.CLIENT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        services.AddEbayTradingAPI(config);
    })
    .UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    })
    .Build();


var ebayService = host.Services.GetRequiredService<EbayTradingService>();

await ebayService.GetSellerListAsync();

