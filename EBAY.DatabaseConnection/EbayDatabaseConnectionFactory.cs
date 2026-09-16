using EBAY.DatabaseConnection.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EBAY.DatabaseConnection;

public sealed class EbayDatabaseConnectionFactory : IEbayDatabaseConnectionFactory
{
    private readonly EBAYDB options;
    private string connectionString;

    public EbayDatabaseConnectionFactory(IOptions<EBAYDB> options)
    {
        this.options = options.Value;
        if (string.IsNullOrWhiteSpace(this.options.ConnectionString))
        {
            throw new InvalidOperationException("Die EBAYDB-Verbindungszeichenfolge ist nicht konfiguriert.");
        }
        ;
        this.connectionString = this.options.ConnectionString;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(connectionString);
    }

    public int CommandTimeOutSeconds => options.CommandTimeoutSeconds;
}
