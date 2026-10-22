using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Order.Entities;
using Domain.Order.Events;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence.Interceptors;

public class DomainEventInterceptorTests
{
    private static DBContext CreateContext(IOutboxEventTypeRegistry registry)
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=localhost;Database=mechanic_interceptor_tests;Username=test;Password=test")
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(registry));
    }

    private static DbContextEventData EventData(DbContext? context)
    {
        var eventData =
            (DbContextEventData)RuntimeHelpers.GetUninitializedObject(typeof(DbContextEventData));

        var field = FindBackingField(eventData.GetType());
        field.ShouldNotBeNull("DbContextEventData should expose a backing field for Context");
        field!.SetValue(eventData, context);

        return eventData;
    }

    private static FieldInfo? FindBackingField(Type? type)
    {
        while (type is not null)
        {
            var field = type.GetField("<Context>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    [Fact]
    public void Type_DerivesFromSaveChangesInterceptor()
    {
        typeof(DomainEventInterceptor).BaseType.ShouldBe(typeof(SaveChangesInterceptor));
    }

    [Fact]
    public async Task SavingChangesAsync_WithNullContext_CompletesWithoutRegistryCalls()
    {
        var registry = Substitute.For<IOutboxEventTypeRegistry>();
        var sut = new DomainEventInterceptor(registry);

        var result = await sut.SavingChangesAsync(EventData(null), new InterceptionResult<int>());

        result.HasResult.ShouldBeFalse();
        registry.DidNotReceiveWithAnyArgs().GetTypeName(default!);
    }

    [Fact]
    public async Task SavingChangesAsync_WithNonDBContext_DoesNotTouchRegistry()
    {
        var registry = Substitute.For<IOutboxEventTypeRegistry>();
        var sut = new DomainEventInterceptor(registry);
        using var plain = new DbContext(new DbContextOptions<DbContext>());

        var result = await sut.SavingChangesAsync(EventData(plain), new InterceptionResult<int>());

        result.HasResult.ShouldBeFalse();
        registry.DidNotReceiveWithAnyArgs().GetTypeName(default!);
    }

    [Fact]
    public async Task SavingChangesAsync_WithUntrackedDBContext_AddsNoOutboxMessages()
    {
        var registry = Substitute.For<IOutboxEventTypeRegistry>();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);

        await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>());

        context.ChangeTracker.Entries<OutboxMessage>().ShouldBeEmpty();
        registry.DidNotReceiveWithAnyArgs().GetTypeName(default!);
    }

    [Fact]
    public async Task SavingChangesAsync_WithTrackedNonEventEntity_AddsNoAdditionalMessages()
    {
        var registry = Substitute.For<IOutboxEventTypeRegistry>();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);

        context.OutboxMessages.Add(OutboxMessage.Create("Seeded", "{}", DateTime.UtcNow));

        await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>());

        context.ChangeTracker.Entries<OutboxMessage>().Count().ShouldBe(1);
        registry.DidNotReceiveWithAnyArgs().GetTypeName(default!);
    }

    [Fact]
    public async Task SavingChangesAsync_WithCancelledToken_StillCompletes()
    {
        var registry = Substitute.For<IOutboxEventTypeRegistry>();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>(), cts.Token);

        result.HasResult.ShouldBeFalse();
    }

    [Fact]
    public void Model_MapsAggregatesThatImplementTheDomainEventContract()
    {
        using var context = CreateContext(Substitute.For<IOutboxEventTypeRegistry>());

        var implementors = context.Model.GetEntityTypes()
            .Where(e => typeof(global::Domain.Common.Abstractions.IHasDomainEvents).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.FullName)
            .ToList();

        implementors.ShouldNotBeEmpty();
    }

    [Fact]
    public void AggregateRoots_ExposeTheirDomainEventsAsAnOrderedList()
    {
        using var context = CreateContext(Substitute.For<IOutboxEventTypeRegistry>());

        var aggregateType = typeof(global::Domain.Common.Abstractions.AggregateRoot<>);

        var nonConforming = context.Model.GetEntityTypes()
            .Where(e => aggregateType.IsAssignableFrom(e.ClrType))
            .Where(e => !typeof(global::Domain.Common.Abstractions.IHasDomainEvents).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.FullName)
            .ToList();

        nonConforming.ShouldBeEmpty();
    }

    [Fact]
    public void SerializerOptions_UseCamelCaseIgnoreNullsAndIgnoreCycles()
    {
        var field = typeof(DomainEventInterceptor).GetField(
            "SerializerOptions", BindingFlags.NonPublic | BindingFlags.Static);

        field.ShouldNotBeNull();
        var options = (JsonSerializerOptions)field!.GetValue(null)!;

        options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        options.DefaultIgnoreCondition.ShouldBe(JsonIgnoreCondition.WhenWritingNull);
        options.ReferenceHandler.ShouldBe(ReferenceHandler.IgnoreCycles);
        options.WriteIndented.ShouldBeFalse();
        options.Encoder.ShouldBe(JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        options.PropertyNameCaseInsensitive.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingChangesAsync_WithTrackedAggregateThatRaisedEvents_AddsAnOutboxRowPerEvent()
    {
        var registry = new OutboxEventTypeRegistry();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);

        var status = OrderStatus.Create("Pending", "در انتظار بررسی", sortOrder: 2);
        context.OrderStatuses.Add(status);

        await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>());

        var messages = context.ChangeTracker.Entries<OutboxMessage>().ToList();

        messages.Count.ShouldBe(1);
        messages[0].Entity.Type.ShouldContain(nameof(OrderStatusCreatedDomainEvent));
        messages[0].Entity.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_WithSeveralEvents_AssignsSameTimestampAndIncreasingSequence()
    {
        var registry = new OutboxEventTypeRegistry();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);

        var first = OrderStatus.Create("Pending", "در انتظار بررسی", sortOrder: 2);
        var second = OrderStatus.Create("Shipped", "ارسال شده", sortOrder: 3);
        context.OrderStatuses.Add(first);
        context.OrderStatuses.Add(second);

        await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>());

        var messages = context.ChangeTracker.Entries<OutboxMessage>()
            .Select(e => e.Entity)
            .ToList();

        messages.Count.ShouldBe(2);
        messages[0].CreatedAt.ShouldBe(messages[1].CreatedAt);
        messages[0].Sequence.ShouldBe(0);
        messages[1].Sequence.ShouldBe(1);
    }

    [Fact]
    public async Task SavingChangesAsync_WithTrackedAggregateWhoseEventsWereCleared_AddsNoOutboxRow()
    {
        var registry = new OutboxEventTypeRegistry();
        var sut = new DomainEventInterceptor(registry);
        using var context = CreateContext(registry);

        var status = OrderStatus.Create("Pending", "در انتظار بررسی", sortOrder: 2);
        status.ClearDomainEvents();
        context.OrderStatuses.Add(status);

        await sut.SavingChangesAsync(EventData(context), new InterceptionResult<int>());

        context.ChangeTracker.Entries<OutboxMessage>().ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_RequiresTypeRegistry()
    {
        var constructor = typeof(DomainEventInterceptor).GetConstructors().Single();

        constructor.GetParameters().Select(p => p.ParameterType)
            .ShouldBe([typeof(IOutboxEventTypeRegistry)]);
    }
}
