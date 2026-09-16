namespace EBAYHttpClient;

public interface IEBayHttpClientFactory
{
    HttpClient GetEbayHttpClient();
}

public sealed class EBayHttpClientFactory : IEBayHttpClientFactory
{
    private readonly IHttpClientFactory httpclientfact;

    public EBayHttpClientFactory(IHttpClientFactory httpclientfact)
    {
        this.httpclientfact = httpclientfact;
    }

    public HttpClient GetEbayHttpClient() => httpclientfact.CreateClient(Constantes.HttpclientProd);
}
