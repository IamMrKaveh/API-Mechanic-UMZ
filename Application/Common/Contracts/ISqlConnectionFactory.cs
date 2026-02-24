namespace Application.Common.Contracts;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}