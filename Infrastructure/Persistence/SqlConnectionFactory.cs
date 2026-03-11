namespace Infrastructure.Persistence;

public class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("PoolerConnection")
            ?? throw new InvalidOperationException("Connection string 'PoolerConnection' not found.");

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}