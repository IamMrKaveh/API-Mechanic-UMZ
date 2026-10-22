using System.Text.Json;
using Application.Audit.Contracts;
using Application.Common.Contracts;
using Domain.Common.Abstractions;
using Domain.Order.Entities;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence.Outbox;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OutboxProcessorTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private OutboxEventTypeRegistry _registry = null!; private IDomainEventDispatcher _dispatcher = null!; private IAuditService _auditService = null!; private OutboxProcessor _sut = null!;

    private static readonly JsonSerializerOptions SerializerOptions = DomainEventJson.Options;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = CreateContext();
        _registry = new OutboxEventTypeRegistry();
        _dispatcher = Substitute.For<IDomainEventDispatcher>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new OutboxProcessor(_context, _dispatcher, _registry, _auditService);
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
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
    }

    private static OutboxMessage BuildMessage(string type, string payload)
        => OutboxMessage.Create(type, payload, DateTime.UtcNow);

    [SkippableFact]
    public async Task ProcessAsync_WithNoPendingMessages_DoesNotDispatch()
    {
        await _sut.ProcessAsync();

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [SkippableFact]
    public async Task ProcessAsync_WithPendingMessageForKnownType_DispatchesEventAndMarksProcessed()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await _sut.ProcessAsync();

        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events!.Single() is OrderExpiredEvent),
            Arg.Any<CancellationToken>());

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        persisted.ProcessedAt.ShouldNotBeNull();
        persisted.IsPoisoned.ShouldBeFalse();
        persisted.RetryCount.ShouldBe(0);
        persisted.Error.ShouldBeNull();
    }

    [SkippableFact]
    public async Task ProcessAsync_WithUnresolvableTypeName_MarksMessageAsPoisoned()
    {
        var message = BuildMessage("Totally.Unknown.Type.Name, Missing.Assembly", "{}");
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await _sut.ProcessAsync();

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        persisted.IsPoisoned.ShouldBeTrue();
        persisted.ProcessedAt.ShouldBeNull();
        persisted.Error.ShouldNotBeNull();
        await _auditService.Received().LogWarningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [SkippableFact]
    public async Task ProcessAsync_WhenDispatchThrows_IncrementsRetryCountAndRecordsError()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _dispatcher
            .When(d => d.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("dispatch failure"));

        await _sut.ProcessAsync();

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        persisted.ProcessedAt.ShouldBeNull();
        persisted.RetryCount.ShouldBe(1);
        persisted.IsPoisoned.ShouldBeFalse();
        persisted.Error.ShouldBe("dispatch failure");
        await _auditService.Received().LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [SkippableFact]
    public async Task ProcessAsync_WhenDispatchFailsAndRetryCountReachesMax_MarksMessageAsPoisoned()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        for (var i = 0; i < 4; i++)
            message.MarkFailed("previous");

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _dispatcher
            .When(d => d.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("still failing"));

        await _sut.ProcessAsync();

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        persisted.RetryCount.ShouldBe(5);
        persisted.IsPoisoned.ShouldBeTrue();
        persisted.ProcessedAt.ShouldBeNull();
        persisted.Error.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task ProcessAsync_SkipsAlreadyProcessedMessages()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);
        message.MarkProcessed(DateTime.UtcNow);

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await _sut.ProcessAsync();

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [SkippableFact]
    public async Task ProcessAsync_SkipsPoisonedMessages()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);
        message.MarkPoisoned("previously poisoned");

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await _sut.ProcessAsync();

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [SkippableFact]
    public async Task ProcessAsync_HonorsBatchSize()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);

        for (var i = 0; i < 3; i++)
            _context.OutboxMessages.Add(BuildMessage(typeName, payload));

        await _context.SaveChangesAsync();

        await _sut.ProcessAsync(batchSize: 2);

        await _dispatcher.Received(2).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events!.Single() is OrderExpiredEvent),
            Arg.Any<CancellationToken>());

        await using var verifyContext = CreateContext();
        var processedCount = await verifyContext.OutboxMessages.CountAsync(m => m.ProcessedAt != null);
        processedCount.ShouldBe(2);
    }

    [SkippableFact]
    public async Task ProcessAsync_WithMessagesOfTheSameTimestamp_DispatchesThemInSequenceOrder()
    {
        var firstEvent = new OrderExpiredEvent(OrderId.NewId());
        var secondEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(firstEvent.GetType());
        var firstPayload = JsonSerializer.Serialize(firstEvent, firstEvent.GetType(), SerializerOptions);
        var secondPayload = JsonSerializer.Serialize(secondEvent, secondEvent.GetType(), SerializerOptions);

        var createdAt = DateTime.UtcNow;

        // The second message is written first, but the sequence keeps the order
        // in which the events were raised.
        var second = OutboxMessage.Create(typeName, secondPayload, createdAt, sequence: 1);
        var first = OutboxMessage.Create(typeName, firstPayload, createdAt, sequence: 0);

        _context.OutboxMessages.Add(second);
        _context.OutboxMessages.Add(first);
        await _context.SaveChangesAsync();

        var dispatchedOrderIds = new List<Guid>();

        _dispatcher
            .When(d => d.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<IDomainEvent>>();
                var expired = (OrderExpiredEvent)events!.Single();
                dispatchedOrderIds.Add(expired.OrderId.Value);
            });

        await _sut.ProcessAsync();

        dispatchedOrderIds.Count.ShouldBe(2);
        dispatchedOrderIds[0].ShouldBe(firstEvent.OrderId.Value);
        dispatchedOrderIds[1].ShouldBe(secondEvent.OrderId.Value);
    }

    [SkippableFact]
    public async Task ProcessAsync_WhenHandlerWritesAndFails_RollsBackItsWritesAndKeepsProcessing()
    {
        var failingEvent = new OrderExpiredEvent(OrderId.NewId());
        var succeedingEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(failingEvent.GetType());
        var failingPayload = JsonSerializer.Serialize(failingEvent, failingEvent.GetType(), SerializerOptions);
        var succeedingPayload = JsonSerializer.Serialize(succeedingEvent, succeedingEvent.GetType(), SerializerOptions);

        var createdAt = DateTime.UtcNow;
        var failing = OutboxMessage.Create(typeName, failingPayload, createdAt, sequence: 0);
        var succeeding = OutboxMessage.Create(typeName, succeedingPayload, createdAt, sequence: 1);

        _context.OutboxMessages.Add(failing);
        _context.OutboxMessages.Add(succeeding);
        await _context.SaveChangesAsync();

        _dispatcher
            .When(d => d.DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                var events = callInfo.Arg<IEnumerable<IDomainEvent>>();
                var expired = (OrderExpiredEvent)events!.Single();

                if (expired.OrderId.Value != failingEvent.OrderId.Value)
                    return;

                // The handler writes, and only then it fails.
                var status = OrderStatus.Create("Pending", "در انتظار بررسی", sortOrder: 2);
                _context.OrderStatuses.Add(status);
                _context.SaveChanges();

                throw new InvalidOperationException("handler failure after write");
            });

        await _sut.ProcessAsync();

        await using var verifyContext = CreateContext();

        // The write of the failed handler is gone.
        (await verifyContext.OrderStatuses.CountAsync()).ShouldBe(0);

        var failedMessage = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == failing.Id);
        failedMessage.RetryCount.ShouldBe(1);
        failedMessage.ProcessedAt.ShouldBeNull();
        failedMessage.IsPoisoned.ShouldBeFalse();
        failedMessage.Error.ShouldNotBeNull();

        // The message after the failed one is still processed.
        var processedMessage = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == succeeding.Id);
        processedMessage.ProcessedAt.ShouldNotBeNull();
        processedMessage.RetryCount.ShouldBe(0);
    }
}
