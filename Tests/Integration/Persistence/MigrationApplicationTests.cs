using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using AuditableEntityInterceptor = Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor;
using DateTimeProvider = Infrastructure.Common.Services.DateTimeProvider;
using DomainEventInterceptor = Infrastructure.Persistence.Interceptors.DomainEventInterceptor;
using OutboxEventTypeRegistry = Infrastructure.Persistence.Outbox.OutboxEventTypeRegistry;

namespace Tests.Integration.Persistence;

/// <summary>
/// Applies the real EF Core migrations to a PostgreSQL container.
///
/// The rest of the integration suite builds its schema with <c>EnsureCreated</c>,
/// which never executes the migration scripts. This test is the only place where a
/// broken or out-of-order migration can be detected.
/// </summary>
[Trait("Category", "Integration")]
public class MigrationApplicationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("mechanic_migration_tests")
                .WithUsername("mechanic")
                .WithPassword("mechanic")
                .WithCleanUp(true)
                .Build();

            await _container.StartAsync();
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
            Skip.If(true, $"No container engine reachable: {ex.GetBaseException().Message}");
        }
    }

    public async Task DisposeAsync()
    {
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

    [SkippableFact]
    public async Task Migrations_ApplyCleanlyToAnEmptyDatabase()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .Options;

        await using var context = new DBContext(
            options,
            new AuditableEntityInterceptor(new DateTimeProvider()),
            new DomainEventInterceptor(new OutboxEventTypeRegistry()));

        await context.Database.MigrateAsync();

        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task Migrations_CreateTheTransactionalOutboxTable()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .Options;

        await using var context = new DBContext(
            options,
            new AuditableEntityInterceptor(new DateTimeProvider()),
            new DomainEventInterceptor(new OutboxEventTypeRegistry()));

        await context.Database.MigrateAsync();

        var tables = await context.Database
            .SqlQueryRaw<string>("SELECT tablename FROM pg_tables WHERE schemaname = 'public'")
            .ToListAsync();

        tables.ShouldContain("OutboxMessages");
        tables.ShouldContain("Orders");
    }
}
