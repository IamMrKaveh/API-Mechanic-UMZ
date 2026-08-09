using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Inventory.Services;

public class InventoryDomainServiceTests
{
    [Fact]
    public void Reserve_DelegatesToAggregateAndReturnsItsResult()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();

        var result = InventoryDomainService.Reserve(
            inv, StockQuantity.Create(4), "REF", OrderItemId.NewId(), UserId.NewId());

        result.ShouldBeSuccess();
        inv.ReservedQuantity.Value.ShouldBe(4);
    }

    [Fact]
    public void ConfirmReservation_DelegatesToAggregateAndReturnsItsResult()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();
        InventoryDomainService.Reserve(inv, StockQuantity.Create(4), "REF").ShouldBeSuccess();

        var result = InventoryDomainService.ConfirmReservation(inv, StockQuantity.Create(4), "REF");

        result.ShouldBeSuccess();
        inv.ReservedQuantity.Value.ShouldBe(0);
        inv.StockQuantity.Value.ShouldBe(6);
    }

    [Fact]
    public void RollbackReservation_DelegatesToAggregateReleaseReservation()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();
        InventoryDomainService.Reserve(inv, StockQuantity.Create(3), "REF").ShouldBeSuccess();

        var result = InventoryDomainService.RollbackReservation(inv, StockQuantity.Create(3), "REF");

        result.ShouldBeSuccess();
        inv.ReservedQuantity.Value.ShouldBe(0);
    }

    [Fact]
    public void ReturnStock_DelegatesToAggregate()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();

        InventoryDomainService.ReturnStock(inv, StockQuantity.Create(2), "return").ShouldBeSuccess();

        inv.StockQuantity.Value.ShouldBe(12);
    }

    [Fact]
    public void RecordDamage_DelegatesToAggregate()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();

        InventoryDomainService.RecordDamage(inv, StockQuantity.Create(3), UserId.NewId(), "broken")
            .ShouldBeSuccess();

        inv.StockQuantity.Value.ShouldBe(7);
    }

    [Fact]
    public void Reconcile_DelegatesToAggregate()
    {
        var inv = new InventoryBuilder().WithInitialStock(10).Build();

        InventoryDomainService.Reconcile(inv, StockQuantity.Create(15), UserId.NewId())
            .ShouldBeSuccess();

        inv.StockQuantity.Value.ShouldBe(15);
    }
}
