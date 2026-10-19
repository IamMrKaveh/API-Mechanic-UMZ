using Infrastructure.Common.Services;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;
using Testcontainers.PostgreSql;

namespace Tests.TestInfrastructure.Database;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    // The container is built lazily (not in a field initializer): PostgreSqlBuilder.Build()
    // validates the Docker endpoint and throws DockerUnavailableException when no engine is
    // reachable. Building it inside InitializeAsync lets that exception be handled, so the
    // database-backed tests skip cleanly instead of erroring on every machine without Docker.
    private PostgreSqlContainer? _container;

    private NpgsqlConnection? _respawnConnection;
    private Respawner? _respawner;
    private static readonly IDateTimeProvider SharedDateTimeProvider = new DateTimeProvider();
    private static readonly IOutboxEventTypeRegistry SharedOutboxRegistry = new OutboxEventTypeRegistry();

    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsDockerAvailable { get; private set; }
    public string? UnavailabilityReason { get; private set; }

    public async Task InitializeAsync()
    {
        PostgreSqlContainer? container = null;
        try
        {
            container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("mechanic_tests")
                .WithUsername("mechanic")
                .WithPassword("mechanic")
                .WithCleanUp(true)
                .Build();

            await container.StartAsync();
            _container = container;
        }
        catch (Exception ex) when (
            ex is DockerUnavailableException
            || ex is InvalidOperationException
            || ex is TimeoutException
            || ex is HttpRequestException
            || (ex is AggregateException agg && agg.InnerExceptions.Any(
                inner => inner is DockerUnavailableException
                         || inner is InvalidOperationException
                         || inner is TimeoutException
                         || inner is HttpRequestException)))
        {
            IsDockerAvailable = false;
            UnavailabilityReason =
                "No Docker-API-compatible engine reachable (Docker Desktop, Podman, Rancher Desktop, Colima, or a remote DOCKER_HOST). "
                + $"Underlying error: {ex.GetBaseException().Message}";
            return;
        }

        try
        {
            ConnectionString = container.GetConnectionString();

            await using var context = CreateContext();
            await context.Database.EnsureCreatedAsync();

            _respawnConnection = new NpgsqlConnection(ConnectionString);
            await _respawnConnection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" }
            });

            IsDockerAvailable = true;
        }
        catch (Exception ex)
        {
            IsDockerAvailable = false;
            UnavailabilityReason = $"Database initialization failed: {ex.GetBaseException().Message}";

            if (_respawnConnection is not null)
                await _respawnConnection.DisposeAsync();

            try
            {
                await container.DisposeAsync();
            }
            catch
            {
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_respawnConnection is not null)
        {
            try
            {
                await _respawnConnection.DisposeAsync();
            }
            catch
            {
            }
        }

        if (_container is null)
            return;

        try
        {
            await _container.DisposeAsync();
        }
        catch
        {
        }
    }

    public DBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(SharedDateTimeProvider),
            new DomainEventInterceptor(SharedOutboxRegistry));
    }

    public async Task ResetAsync()
    {
        if (!IsDockerAvailable || _respawner is null || _respawnConnection is null)
            return;

        await _respawner.ResetAsync(_respawnConnection);
    }
}
