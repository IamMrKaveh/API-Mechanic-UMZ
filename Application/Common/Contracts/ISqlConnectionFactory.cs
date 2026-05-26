namespace Application.Common.Contracts;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}