

using EBAY.InventoryService;
using EBAY.InventoryService.InventoryImport;
using EBAY.TRADING.API.CLIENT;
using EBAYHttpClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Security.Cryptography;
using System.Text.Json;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        services.AddEbayTradingAPI(config);
        services.AddEbayHttpClient(config);
        services.AddEbayInventoryService(config);
    })
    .UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    })
    .Build();

/*
var ebayService = host.Services.GetRequiredService<EbayTradingService>();
await ebayService.GetSellerListAsync();
*/
IInventoryReportImportService srv = host.Services.GetRequiredService<IInventoryReportImportService>();

string fichero = @"D:\Users\dg.rio\Downloads\activeinventory-29399840325634-Sep-15-2026-02-28-41-0700.xml\activeinventory-29399840325634-Sep-15-2026-02-28-41-0700.xml";



byte[] bytes = File.ReadAllBytes(fichero);
string contenido = File.ReadAllText(fichero);


var result = await srv.ImportXmlAsync(contenido, new InventoryReportImportRequest
{
    Source = "SellFeedApi",
    FeedType = "LMS_ACTIVE_INVENTORY_REPORT",
    TaskId = Guid.NewGuid().ToString(),
    SourceFileName = "activeinventory-29399840325634-Sep-15-2026-02-28-41-0700.xml",
    SourceContent = bytes,
    SourceFileSha256 = BitConverter.ToString(SHA256.HashData(bytes)).Replace("-", ""),
    StartedAtLocal = DateTime.Now,
    CompletedAtLocal = DateTime.Now,
    ImportedAtLocal = DateTime.Now
});

Console.WriteLine($"Result: {JsonSerializer.Serialize(result)}");

