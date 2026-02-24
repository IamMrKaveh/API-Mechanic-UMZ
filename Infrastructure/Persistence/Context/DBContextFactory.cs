namespace Infrastructure.Persistence.Context;

public class DBContextFactory : IDesignTimeDbContextFactory<DBContext>
{
    public DBContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PoolerConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Could not find a connection string named 'PoolerConnection'. " +
                "Check your appsettings.json or environment variables.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DBContext(optionsBuilder.Options);
    }
}