using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Application.Audit.Contracts;
using Application.Common.Events;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Persistence.Outbox;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OutboxProcessorTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private OutboxEventTypeRegistry _registry = null!; private IPublisher _publisher = null!; private IAuditService _auditService = null!; private OutboxProcessor _sut = null!;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = CreateContext();
        _registry = new OutboxEventTypeRegistry();
        _publisher = Substitute.For<IPublisher>();
        _auditService = Substitute.For<IAuditService>();
        _sut = new OutboxProcessor(_context, _publisher, _registry, _auditService);
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
    public async Task ProcessAsync_WithNoPendingMessages_DoesNotInvokePublisher()
    {
        await _sut.ProcessAsync();

        await _publisher.DidNotReceiveWithAnyArgs().Publish(default!, Arg.Any<CancellationToken>());
    }

    [SkippableFact]
    public async Task ProcessAsync_WithPendingMessageForKnownType_PublishesNotificationAndMarksProcessed()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await _sut.ProcessAsync();

        await _publisher.Received(1).Publish(
            Arg.Is<object>(o => o is DomainEventNotification<OrderExpiredEvent>),
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
    public async Task ProcessAsync_WhenPublisherThrows_IncrementsRetryCountAndRecordsError()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _publisher
            .When(p => p.Publish(Arg.Any<object>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("publish failure"));

        await _sut.ProcessAsync();

        await using var verifyContext = CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstAsync(m => m.Id == message.Id);
        persisted.ProcessedAt.ShouldBeNull();
        persisted.RetryCount.ShouldBe(1);
        persisted.IsPoisoned.ShouldBeFalse();
        persisted.Error.ShouldBe("publish failure");
        await _auditService.Received().LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [SkippableFact]
    public async Task ProcessAsync_WhenPublisherFailsAndRetryCountReachesMax_MarksMessageAsPoisoned()
    {
        var domainEvent = new OrderExpiredEvent(OrderId.NewId());
        var typeName = _registry.GetTypeName(domainEvent.GetType());
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
        var message = BuildMessage(typeName, payload);

        for (var i = 0; i < 4; i++)
            message.MarkFailed("previous");

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        _publisher
            .When(p => p.Publish(Arg.Any<object>(), Arg.Any<CancellationToken>()))
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

        await _publisher.DidNotReceiveWithAnyArgs().Publish(default!, Arg.Any<CancellationToken>());
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

        await _publisher.DidNotReceiveWithAnyArgs().Publish(default!, Arg.Any<CancellationToken>());
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

        await _publisher.Received(2).Publish(
            Arg.Is<object>(o => o is DomainEventNotification<OrderExpiredEvent>),
            Arg.Any<CancellationToken>());

        await using var verifyContext = CreateContext();
        var processedCount = await verifyContext.OutboxMessages.CountAsync(m => m.ProcessedAt != null);
        processedCount.ShouldBe(2);
    }
}
