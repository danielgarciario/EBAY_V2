using Microsoft.Data.SqlClient;

namespace EBAY.DatabaseConnection
{
    public interface IEbayDatabaseConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}