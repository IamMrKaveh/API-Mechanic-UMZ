using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public void Model_NoMappedEntityImplementsDormantDomainEventContract()
    {
        using var context = CreateContext(Substitute.For<IOutboxEventTypeRegistry>());

        var implementors = context.Model.GetEntityTypes()
            .Where(e => typeof(global::Domain.Common.Abstractions.IHasDomainEvents).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.FullName)
            .ToList();

        implementors.ShouldBeEmpty();
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
    }

    [Fact]
    public void Constructor_RequiresTypeRegistry()
    {
        var constructor = typeof(DomainEventInterceptor).GetConstructors().Single();

        constructor.GetParameters().Select(p => p.ParameterType)
            .ShouldBe([typeof(IOutboxEventTypeRegistry)]);
    }
}
