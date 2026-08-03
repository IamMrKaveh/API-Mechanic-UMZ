using Domain.Discount.Enums;
using Domain.Discount.Events;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Discount.Events;

public class DiscountEventsTests
{
    [Fact]
    public void DiscountCodeCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = DiscountCodeId.NewId();

        var sut = new DiscountCodeCreatedEvent(id, "SAVE10", DiscountType.Percentage, 10m, 100, new DateTime(2026, 12, 31));

        sut.DiscountCodeId.ShouldBe(id);
        sut.Code.ShouldBe("SAVE10");
        sut.Type.ShouldBe(DiscountType.Percentage);
        sut.Value.ShouldBe(10m);
        sut.UsageLimit.ShouldBe(100);
        sut.ExpiresAt.ShouldBe(new DateTime(2026, 12, 31));
    }

    [Fact]
    public void DiscountCodeCreatedEvent_WithNullOptionalArguments_StoresNulls()
    {
        var sut = new DiscountCodeCreatedEvent(DiscountCodeId.NewId(), "X", DiscountType.FreeShipping, 0m, null, null);

        sut.UsageLimit.ShouldBeNull();
        sut.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    public void DiscountCodeAppliedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = DiscountCodeId.NewId();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();

        var sut = new DiscountCodeAppliedEvent(id, "SAVE10", userId, orderId, 25m, 7);

        sut.DiscountCodeId.ShouldBe(id);
        sut.Code.ShouldBe("SAVE10");
        sut.UserId.ShouldBe(userId);
        sut.OrderId.ShouldBe(orderId);
        sut.DiscountedAmount.ShouldBe(25m);
        sut.UsageCount.ShouldBe(7);
    }

    [Fact]
    public void DiscountCodeActivatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = DiscountCodeId.NewId();

        var sut = new DiscountCodeActivatedEvent(id, "SAVE10");

        sut.DiscountCodeId.ShouldBe(id);
        sut.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public void DiscountCodeDeactivatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = DiscountCodeId.NewId();

        var sut = new DiscountCodeDeactivatedEvent(id, "SAVE10");

        sut.DiscountCodeId.ShouldBe(id);
        sut.Code.ShouldBe("SAVE10");
    }
}
