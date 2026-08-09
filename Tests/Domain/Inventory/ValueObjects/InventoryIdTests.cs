using Domain.Inventory.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Inventory.ValueObjects;

public class InventoryIdTests
{
    [Fact]
    public void NewId_Always_ReturnsNonEmptyGuid()
    {
        var id = InventoryId.NewId();

        id.ShouldNotBeNull();
        id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewId_TwoCalls_ProduceDistinctValues()
    {
        var a = InventoryId.NewId();
        var b = InventoryId.NewId();

        a.Value.ShouldNotBe(b.Value);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        var id = InventoryId.From(guid);

        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => InventoryId.From(Guid.Empty));

        ex.Message.ShouldBe("InventoryId cannot be empty.");
    }

    [Fact]
    public void ToString_ReturnsGuidStringRepresentation()
    {
        var guid = Guid.NewGuid();
        var id = InventoryId.From(guid);

        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void ImplicitOperator_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var id = InventoryId.From(guid);

        Guid extracted = id;

        extracted.ShouldBe(guid);
    }

    [Fact]
    public void Equality_TwoIdsWithSameGuid_TreatedAsEqual()
    {
        var guid = Guid.NewGuid();

        var a = InventoryId.From(guid);
        var b = InventoryId.From(guid);

        a.ShouldBe(b);
    }
}
