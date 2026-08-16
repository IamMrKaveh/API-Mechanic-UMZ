using Domain.Order.Events;
using Infrastructure.Persistence.Outbox;

namespace Tests.Infrastructure.Persistence.Outbox;

public class OutboxEventTypeRegistryTests
{
    private readonly OutboxEventTypeRegistry _sut = new();

    [Fact]
    public void GetTypeName_ForRegisteredDomainEvent_ReturnsAssemblyQualifiedName()
    {
        var eventType = typeof(OrderExpiredEvent);

        var typeName = _sut.GetTypeName(eventType);

        typeName.ShouldBe($"{eventType.FullName}, {eventType.Assembly.GetName().Name}");
    }

    [Fact]
    public void GetTypeName_ForUnregisteredType_ReturnsFallbackAssemblyQualifiedName()
    {
        var typeName = _sut.GetTypeName(typeof(OutboxEventTypeRegistryTests));

        typeName.ShouldBe($"{typeof(OutboxEventTypeRegistryTests).FullName}, {typeof(OutboxEventTypeRegistryTests).Assembly.GetName().Name}");
    }

    [Fact]
    public void Resolve_ForRegisteredAssemblyQualifiedName_ReturnsMatchingType()
    {
        var eventType = typeof(OrderExpiredEvent);
        var typeName = _sut.GetTypeName(eventType);

        _sut.Resolve(typeName).ShouldBe(eventType);
    }

    [Fact]
    public void Resolve_ForRegisteredFullNameWithoutAssembly_ReturnsMatchingType()
    {
        var eventType = typeof(OrderExpiredEvent);

        _sut.Resolve(eventType.FullName!).ShouldBe(eventType);
    }

    [Fact]
    public void Resolve_ForFullNameFragmentOfAssemblyQualifiedName_ReturnsMatchingType()
    {
        var eventType = typeof(OrderExpiredEvent);
        var typeName = $"{eventType.FullName}, NonExistent.Assembly";

        _sut.Resolve(typeName).ShouldBe(eventType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ForBlankInput_ReturnsNull(string typeName)
    {
        _sut.Resolve(typeName).ShouldBeNull();
    }

    [Fact]
    public void Resolve_ForNullInput_ReturnsNull()
    {
        _sut.Resolve(null!).ShouldBeNull();
    }

    [Fact]
    public void Resolve_ForCompletelyUnknownName_ReturnsNull()
    {
        _sut.Resolve("Completely.Unknown.Type.Name, Missing.Assembly").ShouldBeNull();
    }

    [Fact]
    public void Ctor_ScansForConcreteDomainEventTypes()
    {
        _sut.Resolve(typeof(OrderExpiredEvent).FullName!).ShouldNotBeNull();
    }
}
