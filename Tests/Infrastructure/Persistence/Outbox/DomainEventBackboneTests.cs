using Application.Common.Contracts;
using Domain.Common.Abstractions;
using Domain.Order.Entities;
using Domain.Order.Events;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence.Outbox;

/// <summary>
/// Verifies the full domain event backbone:
/// aggregate raises event → interceptor writes the outbox row → outbox processor
/// reads it back → dispatcher publishes it to the notification handlers.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DomainEventBackboneTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private OutboxEventTypeRegistry _registry = null!;
    private IDomainEventDispatcher _dispatcher = null!;
    private IAuditService _auditService = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = CreateContext();
        _registry = new OutboxEventTypeRegistry();
        _dispatcher = Substitute.For<IDomainEventDispatcher>();
        _auditService = Substitute.For<IAuditService>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private DBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(new OutboxEventTypeRegistry()));
    }

    [SkippableFact]
    public async Task AggregateRaisingEvent_IsCapturedAsAnOutboxRow()
    {
        var status = OrderStatus.Create("Pending", "در انتظار بررسی", sortOrder: 2);

        _context.OrderStatuses.Add(status);
        await _context.SaveChangesAsync();

        var outboxRow = await _context.OutboxMessages.SingleAsync();

        outboxRow.Type.ShouldContain(nameof(OrderStatusCreatedDomainEvent));
        outboxRow.ProcessedAt.ShouldBeNull();
        outboxRow.IsPoisoned.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task OutboxRow_IsDispatchedToTheHandlersByTheProcessor()
    {
        var status = OrderStatus.Create("Shipped", "ارسال شده", sortOrder: 3);

        _context.OrderStatuses.Add(status);
        await _context.SaveChangesAsync();

        var dispatched = new List<IDomainEvent>();

        _dispatcher
            .When(d => d.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>()))
            .Do(call => dispatched.AddRange(call.Arg<IEnumerable<IDomainEvent>>()!));

        var processor = new OutboxProcessor(_context, _dispatcher, _registry, _auditService);

        await processor.ProcessAsync();

        dispatched.Count.ShouldBe(1);
        var created = dispatched.Single().ShouldBeOfType<OrderStatusCreatedDomainEvent>();
        created.OrderStatusId.ShouldBe(status.Id);
        created.Name.ShouldBe("Shipped");

        var persisted = await _context.OutboxMessages.SingleAsync();
        persisted.ProcessedAt.ShouldNotBeNull();
        persisted.Error.ShouldBeNull();
    }

    [SkippableFact]
    public async Task AggregateWithoutEvents_ProducesNoOutboxRow()
    {
        var status = OrderStatus.Create("Delivered", "تحویل شده", sortOrder: 4);
        status.ClearDomainEvents();

        _context.OrderStatuses.Add(status);
        await _context.SaveChangesAsync();

        (await _context.OutboxMessages.CountAsync()).ShouldBe(0);
    }
}
