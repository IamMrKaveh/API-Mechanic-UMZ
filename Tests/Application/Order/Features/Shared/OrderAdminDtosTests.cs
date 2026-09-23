using Application.Order.Features.Shared;

namespace Tests.Application.Order.Features.Shared;

public class OrderAdminDtosTests
{
    [Fact]
    public void AdminOrderDto_Defaults_AreEmpty()
    {
        var dto = new AdminOrderDto();

        dto.Id.ShouldBe(default(Guid));
        dto.UserId.ShouldBe(default(Guid));
        dto.OrderNumber.ShouldBe(string.Empty);
        dto.ReceiverName.ShouldBe(string.Empty);
        dto.Status.ShouldBe(string.Empty);
        dto.TotalAmount.ShouldBe(0m);
        dto.DiscountCodeId.ShouldBeNull();
        dto.PaymentDate.ShouldBeNull();
        dto.UserAddress.ShouldBeNull();
        dto.OrderItems.ShouldNotBeNull();
        dto.OrderItems.ShouldBeEmpty();
        dto.StatusDisplayName.ShouldBe(string.Empty);
        dto.Shipping.ShouldBeNull();
        dto.AllowedTransitions.ShouldNotBeNull();
        dto.AllowedTransitions.ShouldBeEmpty();
        dto.User.ShouldBeNull();
        dto.RowVersion.ShouldBe(string.Empty);
    }

    [Fact]
    public void AdminOrderDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var dto = new AdminOrderDto
        {
            Id = id,
            UserId = Guid.NewGuid(),
            OrderNumber = "ORD-1001",
            ReceiverName = "Ali Rezaei",
            OrderStatusId = Guid.NewGuid(),
            Status = "Paid",
            TotalAmount = 500_000m,
            TotalProfit = 100_000m,
            ShippingCost = 50_000m,
            DiscountAmount = 20_000m,
            FinalAmount = 530_000m,
            ShippingId = Guid.NewGuid(),
            IsPaid = true,
            IsCancelled = false,
            IsCancellable = true,
            OrderItemsCount = 2,
            IsDeleted = false
        };

        dto.Id.ShouldBe(id);
        dto.OrderNumber.ShouldBe("ORD-1001");
        dto.FinalAmount.ShouldBe(530_000m);
        dto.IsPaid.ShouldBeTrue();
    }

    [Fact]
    public void UserSummaryDto_RoundTrip()
    {
        var dto = new UserSummaryDto
        {
            Id = Guid.NewGuid(), PhoneNumber = "09120000000",
            FirstName = "Ali", LastName = "Rezaei", IsAdmin = true
        };

        dto.PhoneNumber.ShouldBe("09120000000");
        dto.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void UpdateOrderDto_NullableShippingId_AcceptsValueAndNull()
    {
        var withValue = new UpdateOrderDto { ShippingId = Guid.NewGuid() };
        withValue.ShippingId.ShouldNotBeNull();

        var without = new UpdateOrderDto { ShippingId = null };
        without.ShippingId.ShouldBeNull();
    }

    [Fact]
    public void AdminCreateOrderItemDto_RoundTrip()
    {
        var dto = new AdminCreateOrderItemDto
        {
            VariantId = Guid.NewGuid(), Quantity = 3, SellingPrice = 150_000m
        };

        dto.Quantity.ShouldBe(3);
        dto.SellingPrice.ShouldBe(150_000m);
    }

    [Fact]
    public void OrderStatisticsDto_RoundTrip()
    {
        var dto = new OrderStatisticsDto
        {
            TotalOrders = 100, PendingOrders = 10, ProcessingOrders = 20,
            CompletedOrders = 60, CancelledOrders = 10,
            TotalRevenue = 50_000_000m, AverageOrderValue = 500_000m
        };

        dto.TotalOrders.ShouldBe(100);
        dto.AverageOrderValue.ShouldBe(500_000m);
    }

    [Fact]
    public void AdminOrderDto_WithExpression_PreservesOthers()
    {
        var dto = new AdminOrderDto { Id = Guid.NewGuid(), Status = "Paid", IsPaid = true };

        var updated = dto with { Status = "Shipped" };

        updated.Status.ShouldBe("Shipped");
        updated.IsPaid.ShouldBeTrue();
    }
}
