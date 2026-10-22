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

    [SkippableFact]
    public async Task SaveChangesAsync_WithPendingInsert_PersistsChangesToDatabase()
    {
        var message = OutboxMessage.Create("SampleEvent", "{}", DateTime.UtcNow);
        _context.OutboxMessages.Add(message);

        var sut = new UnitOfWork(_context, _logger);

        await sut.SaveChangesAsync();

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldNotBeNull();
        persisted.Type.ShouldBe("SampleEvent");
    }

    [SkippableFact]
    public async Task ExecuteStrategyAsync_WhenOperationSucceeds_CommitsChanges()
    {
        var sut = new UnitOfWork(_context, _logger);
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

    [SkippableFact]
    public async Task ExecuteStrategyAsync_WhenOperationThrows_RollsBackChangesAndRethrows()
    {
        var sut = new UnitOfWork(_context, _logger);
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

    [SkippableFact]
    public async Task ExecuteStrategyAsync_WithActiveTransaction_JoinsTheAmbientTransaction()
    {
        var sut = new UnitOfWork(_context, _logger);

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

    [SkippableFact]
    public async Task ExecuteStrategyAsync_WithActiveTransactionAndFailingOperation_RollsBackToTheSavepoint()
    {
        var sut = new UnitOfWork(_context, _logger);
        var message = OutboxMessage.Create("SavepointEvent", "{}", DateTime.UtcNow);

        await using var outer = await _context.Database.BeginTransactionAsync();

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.ExecuteStrategyAsync<int>(async ct =>
            {
                _context.OutboxMessages.Add(message);
                await _context.SaveChangesAsync(ct);
                throw new InvalidOperationException("intentional");
            }));

        // The savepoint rolled the failed operation back, so its row is gone even
        // though the ambient transaction is still open.
        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldBeNull();

        await outer.RollbackAsync();
    }

    [SkippableFact]
    public async Task ExecuteTransactionAsync_WhenTheResultIsRejected_RollsBackTheChanges()
    {
        var sut = new UnitOfWork(_context, _logger);
        var message = OutboxMessage.Create("RejectedEvent", "{}", DateTime.UtcNow);

        var result = await sut.ExecuteTransactionAsync<string>(
            async ct =>
            {
                _context.OutboxMessages.Add(message);
                await _context.SaveChangesAsync(ct);
                return "rejected";
            },
            _ => false);

        result.ShouldBe("rejected");

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldBeNull();
    }

    [SkippableFact]
    public async Task ExecuteTransactionAsync_WhenTheResultIsAccepted_CommitsTheChanges()
    {
        var sut = new UnitOfWork(_context, _logger);
        var message = OutboxMessage.Create("AcceptedEvent", "{}", DateTime.UtcNow);

        var result = await sut.ExecuteTransactionAsync<string>(
            async ct =>
            {
                _context.OutboxMessages.Add(message);
                await _context.SaveChangesAsync(ct);
                return "accepted";
            },
            _ => true);

        result.ShouldBe("accepted");

        await using var verifyContext = CreateFreshContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task SaveChangesAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var sut = new UnitOfWork(_context, _logger);
        sut.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await sut.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task ExecuteStrategyAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var sut = new UnitOfWork(_context, _logger);
        sut.Dispose();

        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await sut.ExecuteStrategyAsync(_ => Task.FromResult(0)));
    }

    [SkippableFact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var sut = new UnitOfWork(_context, _logger);

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        await Should.ThrowAsync<ObjectDisposedException>(async () => await sut.SaveChangesAsync());
    }
}
