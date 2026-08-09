using Domain.Inventory.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Inventory.ValueObjects;

public class WarehouseIdTests
{
    [Fact]
    public void NewId_Always_ReturnsNonEmptyGuid()
    {
        WarehouseId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        WarehouseId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => WarehouseId.From(Guid.Empty));

        ex.Message.ShouldBe("WarehouseId cannot be empty.");
    }

    [Fact]
    public void ImplicitOperator_ToGuid_ReturnsUnderlyingValue()
    {
        var guid = Guid.NewGuid();
        var id = WarehouseId.From(guid);

        Guid extracted = id;

        extracted.ShouldBe(guid);
    }
}
