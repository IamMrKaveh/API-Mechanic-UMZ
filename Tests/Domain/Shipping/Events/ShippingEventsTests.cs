using Domain.Shipping.Events;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Shipping.Events;

public class ShippingEventsTests
{
    [Fact]
    public void ShippingCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = ShippingId.NewId();
        var name = ShippingName.Create("Express");
        const decimal baseCost = 50_000m;

        var sut = new ShippingCreatedEvent(id, name, baseCost);

        sut.ShippingId.ShouldBe(id);
        sut.Name.ShouldBe(name);
        sut.BaseCost.ShouldBe(baseCost);
    }

    [Fact]
    public void ShippingUpdatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = ShippingId.NewId();
        var name = ShippingName.Create("Standard");

        var sut = new ShippingUpdatedEvent(id, name);

        sut.ShippingId.ShouldBe(id);
        sut.Name.ShouldBe(name);
    }

    [Fact]
    public void ShippingCostChangedEvent_ExposesPreviousAndNewCost()
    {
        var sut = new ShippingCostChangedEvent(ShippingId.NewId(), 50_000m, 80_000m);

        sut.PreviousCost.ShouldBe(50_000m);
        sut.NewCost.ShouldBe(80_000m);
    }

    [Fact]
    public void ShippingSetAsDefaultEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = ShippingId.NewId();
        var name = ShippingName.Create("Default");

        var sut = new ShippingSetAsDefaultEvent(id, name);

        sut.ShippingId.ShouldBe(id);
        sut.Name.ShouldBe(name);
    }

    [Fact]
    public void ShippingDeletedEvent_WithDeleter_StoresDeleter()
    {
        var deleter = UserId.NewId();

        var sut = new ShippingDeletedEvent(ShippingId.NewId(), deleter);

        sut.DeletedBy.ShouldBe(deleter);
    }

    [Fact]
    public void ShippingDeletedEvent_WithoutDeleter_StoresNull()
    {
        var sut = new ShippingDeletedEvent(ShippingId.NewId(), null);

        sut.DeletedBy.ShouldBeNull();
    }
}
