using Microsoft.EntityFrameworkCore.Design;

namespace Tests.Infrastructure.Persistence.Context;

[CollectionDefinition("DBContextFactorySequential", DisableParallelization = true)]
public sealed class DBContextFactorySequentialDefinition;

[Collection("DBContextFactorySequential")]
public class DBContextFactoryTests
{
    private static readonly Lock Gate = new();

    private static string WriteAppsettings(string directory, string? migrationConnection, string? envOverride = null, string? envName = null)
    {
        var main = "{\"ConnectionStrings\":{}}";
        if (migrationConnection is not null)
            main = "{\"ConnectionStrings\":{\"MigrationConnection\":\"" + migrationConnection + "\"}}";

        File.WriteAllText(Path.Combine(directory, "appsettings.json"), main);

        if (envOverride is not null && envName is not null)
        {
            var envContent = "{\"ConnectionStrings\":{\"MigrationConnection\":\"" + envOverride + "\"}}";
            File.WriteAllText(Path.Combine(directory, $"appsettings.{envName}.json"), envContent);
        }

        return directory;
    }

    private static string RunInTempDirectory(Func<string, string?> action)
    {
        lock (Gate)
        {
            var previousDirectory = Environment.CurrentDirectory;
            var previousEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var tempDirectory = Path.Combine(Path.GetTempPath(), "dbfactory-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                Environment.CurrentDirectory = tempDirectory;
                var result = action(tempDirectory);
                return result ?? string.Empty;
            }
            finally
            {
                Environment.CurrentDirectory = previousDirectory;
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnv);
                try { Directory.Delete(tempDirectory, recursive: true); } catch { }
            }
        }
    }

    [Fact]
    public void Type_ImplementsDesignTimeFactory()
    {
        typeof(IDesignTimeDbContextFactory<DBContext>)
            .IsAssignableFrom(typeof(DBContextFactory))
            .ShouldBeTrue();
    }

    [Fact]
    public void CreateDbContext_WhenAppsettingsIsMissing_ThrowsFileNotFoundException()
    {
        RunInTempDirectory(_ =>
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            Should.Throw<FileNotFoundException>(() => sut.CreateDbContext([]));
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WhenMigrationConnectionIsMissing_ThrowsInvalidOperationException()
    {
        RunInTempDirectory(dir =>
        {
            WriteAppsettings(dir, migrationConnection: null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            var ex = Should.Throw<InvalidOperationException>(() => sut.CreateDbContext([]));
            ex.Message.ShouldContain("MigrationConnection");
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WhenMigrationConnectionIsWhitespace_ThrowsInvalidOperationException()
    {
        RunInTempDirectory(dir =>
        {
            WriteAppsettings(dir, "   ");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            Should.Throw<InvalidOperationException>(() => sut.CreateDbContext([]));
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WithValidConnectionString_ReturnsNpgsqlContext()
    {
        RunInTempDirectory(dir =>
        {
            const string connectionString = "Host=localhost;Database=mechanic_migrations;Username=migrator;Password=secret";
            WriteAppsettings(dir, connectionString);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            using var context = sut.CreateDbContext([]);
            context.ShouldNotBeNull();
            context.Database.ProviderName.ShouldContain("Npgsql");
            context.Database.GetConnectionString().ShouldBe(connectionString);
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WhenEnvironmentSpecificFileExists_PrefersEnvironmentConnectionString()
    {
        RunInTempDirectory(dir =>
        {
            const string baseConnection = "Host=localhost;Database=base;Username=migrator;Password=secret";
            const string envConnection = "Host=localhost;Database=env_override;Username=migrator;Password=secret";
            const string envName = "FactoryTestEnv";

            WriteAppsettings(dir, baseConnection, envOverride: envConnection, envName: envName);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", envName);

            var sut = new DBContextFactory();

            using var context = sut.CreateDbContext([]);
            context.Database.GetConnectionString().ShouldBe(envConnection);
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WhenEnvironmentSpecificFileIsAbsent_FallsBackToBaseConnectionString()
    {
        RunInTempDirectory(dir =>
        {
            const string baseConnection = "Host=localhost;Database=base;Username=migrator;Password=secret";
            WriteAppsettings(dir, baseConnection);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "MissingEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            using var context = sut.CreateDbContext([]);
            context.Database.GetConnectionString().ShouldBe(baseConnection);
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_WhenAppsettingsIsMalformed_Throws()
    {
        RunInTempDirectory(dir =>
        {
            File.WriteAllText(Path.Combine(dir, "appsettings.json"), "{ malformed json");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            Should.Throw<Exception>(() => sut.CreateDbContext([]));
            return null;
        });
    }

    [Fact]
    public void CreateDbContext_IgnoresArgsContent()
    {
        RunInTempDirectory(dir =>
        {
            const string connectionString = "Host=localhost;Database=args_ignored;Username=migrator;Password=secret";
            WriteAppsettings(dir, connectionString);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnconfiguredEnv" + Guid.NewGuid().ToString("N"));

            var sut = new DBContextFactory();

            using var first = sut.CreateDbContext([]);
            using var second = sut.CreateDbContext(["--", "anything"]);
            first.Database.GetConnectionString().ShouldBe(second.Database.GetConnectionString());
            return null;
        });
    }
}
