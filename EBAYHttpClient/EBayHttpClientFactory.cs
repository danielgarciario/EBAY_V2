namespace EBAYHttpClient;

public interface IEBayHttpClientFactory
{
    HttpClient GetEbayHttpClient();
    HttpClient GetEbayTradingApiHttpClient();
}

public sealed class EBayHttpClientFactory : IEBayHttpClientFactory
{
    private readonly IHttpClientFactory httpclientfact;

    public EBayHttpClientFactory(IHttpClientFactory httpclientfact)
    {
        this.httpclientfact = httpclientfact;
    }

    public HttpClient GetEbayHttpClient() => httpclientfact.CreateClient(Constantes.HttpclientProd);

    public HttpClient GetEbayTradingApiHttpClient() => httpclientfact.CreateClient(Constantes.HttpclientTrading);
}
