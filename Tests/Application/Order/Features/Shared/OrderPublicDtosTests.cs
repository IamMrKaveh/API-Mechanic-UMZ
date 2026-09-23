using Application.Order.Features.Shared;

namespace Tests.Application.Order.Features.Shared;

public class OrderPublicDtosTests
{
    [Fact]
    public void OrderDto_Defaults_AreEmpty()
    {
        var dto = new OrderDto();

        dto.Id.ShouldBe(default(Guid));
        dto.OrderNumber.ShouldBe(string.Empty);
        dto.Status.ShouldBe(string.Empty);
        dto.StatusDisplayName.ShouldBe(string.Empty);
        dto.SubTotal.ShouldBe(0m);
        dto.AllowedTransitions.ShouldNotBeNull();
        dto.AllowedTransitions.ShouldBeEmpty();
        dto.CancellationReason.ShouldBeNull();
        dto.ReceiverInfo.ShouldBeNull();
        dto.DeliveryAddress.ShouldBeNull();
        dto.Items.ShouldNotBeNull();
        dto.Items.ShouldBeEmpty();
    }

    [Fact]
    public void OrderDto_WithItems_PreservesCollection()
    {
        var dto = new OrderDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-5",
            Items = new List<OrderItemDto>
            {
                new() { Id = Guid.NewGuid(), ProductName = "Brake Pad", Sku = "SKU-1", UnitPrice = 100m, Quantity = 2, TotalPrice = 200m }
            }
        };

        dto.Items.Count.ShouldBe(1);
        dto.Items[0].TotalPrice.ShouldBe(200m);
    }

    [Fact]
    public void OrderListItemDto_RoundTrip()
    {
        var dto = new OrderListItemDto
        {
            Id = Guid.NewGuid(), OrderNumber = "ORD-9", Status = "Delivered",
            StatusDisplayName = "تحویل داده شده", FinalAmount = 300_000m,
            ItemCount = 3, CreatedAt = new DateTime(2026, 1, 10)
        };

        dto.Status.ShouldBe("Delivered");
        dto.ItemCount.ShouldBe(3);
    }

    [Fact]
    public void OrderItemDto_Defaults_AreEmpty()
    {
        var dto = new OrderItemDto();

        dto.ProductName.ShouldBe(string.Empty);
        dto.Sku.ShouldBe(string.Empty);
        dto.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void ReceiverInfoDto_RoundTrip()
    {
        var dto = new ReceiverInfoDto { FullName = "Sara Ahmadi", PhoneNumber = "09120000000" };

        dto.FullName.ShouldBe("Sara Ahmadi");
        dto.PhoneNumber.ShouldBe("09120000000");
    }

    [Fact]
    public void DeliveryAddressDto_RoundTrip()
    {
        var dto = new DeliveryAddressDto
        {
            Province = "Tehran", City = "Tehran",
            AddressLine = "Valiasr St. 10", PostalCode = "1234567890"
        };

        dto.PostalCode.ShouldBe("1234567890");
    }

    [Fact]
    public void OrderDto_ValueEquality_Works()
    {
        var id = Guid.NewGuid();
        var a = new OrderListItemDto { Id = id, OrderNumber = "X", Status = "Paid", FinalAmount = 10m, ItemCount = 1, CreatedAt = default };
        var b = new OrderListItemDto { Id = id, OrderNumber = "X", Status = "Paid", FinalAmount = 10m, ItemCount = 1, CreatedAt = default };

        a.ShouldBe(b);
    }
}
