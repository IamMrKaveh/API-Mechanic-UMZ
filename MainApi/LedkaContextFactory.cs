namespace MainApi;

public class LedkaContextFactory : IDesignTimeDbContextFactory<LedkaContext>
{
    public LedkaContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<LedkaContext>();

        var connectionString = configuration.GetConnectionString("PoolerConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Database=LedkaDb;Username=postgres;Password=password";
        }

        builder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            npgsqlOptions.SetPostgresVersion(new Version(15, 0));
        });

        return new LedkaContext(builder.Options);
    }
}