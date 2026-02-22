namespace Infrastructure.Persistence.Context;

public class DesignTimeFactoryLedkaContext : IDesignTimeDbContextFactory<LedkaContext>
{
    public LedkaContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("PoolerConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Could not find a connection string named 'PoolerConnection'. " +
                "Check your appsettings.json file.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<LedkaContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new LedkaContext(optionsBuilder.Options);
    }
}