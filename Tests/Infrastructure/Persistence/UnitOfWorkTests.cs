using Infrastructure.Persistence;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Hosting;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class UnitOfWorkTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private ILogger<UnitOfWork> _logger = null!; private IHostEnvironment _environment = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _logger = Substitute.For<ILogger<UnitOfWork>>();
        _environment = Substitute.For<IHostEnvironment>();
        _environment.EnvironmentName = Environments.Production;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _fixture.ResetAsync();
    }

    private DBContext CreateFreshContext()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
    }

    [Fact]
    public async Task SaveChangesAsync_WithPendingInsert_PersistsChangesToDatabase()
    {
        var message = OutboxMessage.Create("SampleEvent", "{}", DateTime.UtcNow);
        _context.OutboxMessages.Add(message);

        var sut = new UnitOfWork(_context, _logger, _environment);

        await sut.SaveChangesAsync();

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldNotBeNull();
        persisted.Type.ShouldBe("SampleEvent");
    }

    [Fact]
    public async Task ExecuteStrategyAsync_WhenOperationSucceeds_CommitsChanges()
    {
        var sut = new UnitOfWork(_context, _logger, _environment);
        var message = OutboxMessage.Create("SampleEvent", "{}", DateTime.UtcNow);

        var result = await sut.ExecuteStrategyAsync(async ct =>
        {
            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync(ct);
            return message.Id;
        });

        result.ShouldBe(message.Id);

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteStrategyAsync_WhenOperationThrows_RollsBackChangesAndRethrows()
    {
        var sut = new UnitOfWork(_context, _logger, _environment);
        var message = OutboxMessage.Create("SampleEvent", "{}", DateTime.UtcNow);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.ExecuteStrategyAsync<int>(async ct =>
            {
                _context.OutboxMessages.Add(message);
                await _context.SaveChangesAsync(ct);
                throw new InvalidOperationException("intentional");
            }));

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteStrategyAsync_WithActiveTransactionInDevelopment_ThrowsInvalidOperationException()
    {
        _environment.EnvironmentName = Environments.Development;
        var sut = new UnitOfWork(_context, _logger, _environment);

        await using var outer = await _context.Database.BeginTransactionAsync();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.ExecuteStrategyAsync<int>(_ => Task.FromResult(1)));

        await outer.RollbackAsync();
    }

    [Fact]
    public async Task ExecuteStrategyAsync_WithActiveTransactionInProduction_ExecutesOperationWithoutRetryWrapper()
    {
        _environment.EnvironmentName = Environments.Production;
        var sut = new UnitOfWork(_context, _logger, _environment);

        await using var outer = await _context.Database.BeginTransactionAsync();

        var executionCount = 0;

        var result = await sut.ExecuteStrategyAsync(_ =>
        {
            executionCount++;
            return Task.FromResult(42);
        });

        result.ShouldBe(42);
        executionCount.ShouldBe(1);

        await outer.RollbackAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var sut = new UnitOfWork(_context, _logger, _environment);
        sut.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await sut.SaveChangesAsync());
    }

    [Fact]
    public async Task ExecuteStrategyAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var sut = new UnitOfWork(_context, _logger, _environment);
        sut.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await sut.ExecuteStrategyAsync(_ => Task.FromResult(0)));
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var sut = new UnitOfWork(_context, _logger, _environment);

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await sut.SaveChangesAsync());
    }
}
