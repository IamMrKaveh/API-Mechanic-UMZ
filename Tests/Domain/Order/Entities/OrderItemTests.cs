using Domain.Order.Entities;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Order.Entities;

public class OrderItemTests
{
    [Fact]
    public void Place_WithSingleSnapshot_ProducesInitializedOrderItem()
    {
        var snapshot = new OrderItemSnapshotBuilder()
            .WithProductName(ProductName.Create("Product X"))
            .WithSku(Sku.Create("SKU-X-01"))
            .WithUnitPrice(200m)
            .WithQuantity(2)
            .Build();
        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();

        var sut = order.OrderItems.Single();

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.OrderId.ShouldBe(order.Id);
        sut.VariantId.ShouldBe(snapshot.VariantId);
        sut.ProductId.ShouldBe(snapshot.ProductId);
        sut.ProductName.ShouldBe("Product X");
        sut.Sku.ShouldBe("SKU-X-01");
        sut.UnitPrice.Amount.ShouldBe(200m);
        sut.Quantity.ShouldBe(2);
    }

    [Fact]
    public void Place_CopiesUnitPriceInsteadOfSharingReference()
    {
        var unitPrice = Money.Create(500m, "IRT");
        var snapshot = new OrderItemSnapshotBuilder().WithUnitPrice(unitPrice).WithQuantity(1).Build();

        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();
        var sut = order.OrderItems.Single();

        sut.UnitPrice.ShouldNotBeSameAs(unitPrice);
        sut.UnitPrice.Amount.ShouldBe(500m);
    }

    [Fact]
    public void TotalPrice_ReturnsUnitPriceMultipliedByQuantity()
    {
        var snapshot = new OrderItemSnapshotBuilder().WithUnitPrice(250m).WithQuantity(4).Build();
        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();

        var sut = order.OrderItems.Single();

        sut.TotalPrice.Amount.ShouldBe(1_000m);
        sut.TotalPrice.Currency.ShouldBe(sut.UnitPrice.Currency);
    }

    [Fact]
    public void Place_WithMultipleSnapshots_ProducesOrderItemWithDistinctIdentityPerSnapshot()
    {
        var first = new OrderItemSnapshotBuilder().Build();
        var second = new OrderItemSnapshotBuilder().Build();

        var order = new OrderBuilder().WithItemSnapshots(first, second).Build();

        order.OrderItems.Count.ShouldBe(2);
        var ids = order.OrderItems.Select(i => i.Id).ToList();
        ids.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void Place_AttachesOrderIdToEveryProducedOrderItem()
    {
        var first = new OrderItemSnapshotBuilder().Build();
        var second = new OrderItemSnapshotBuilder().Build();

        var order = new OrderBuilder().WithItemSnapshots(first, second).Build();

        order.OrderItems.ShouldAllBe(i => i.OrderId == order.Id);
    }

    [Fact]
    public void Place_PreservesProductAndVariantReferencesFromSnapshot()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();
        var snapshot = new OrderItemSnapshotBuilder()
            .WithVariantId(variantId)
            .WithProductId(productId)
            .Build();

        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();
        var sut = order.OrderItems.Single();

        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void SubTotal_EqualsSumOfOrderItemTotalPrices()
    {
        var first = new OrderItemSnapshotBuilder().WithUnitPrice(100m).WithQuantity(2).Build();
        var second = new OrderItemSnapshotBuilder().WithUnitPrice(50m).WithQuantity(3).Build();

        var order = new OrderBuilder()
            .WithItemSnapshots(first, second)
            .WithShippingCost(0m)
            .WithDiscountAmount(0m)
            .Build();

        order.SubTotal.Amount.ShouldBe(350m);
    }

    [Fact]
    public void OrderItems_AreExposedAsReadOnlyCollection()
    {
        var order = new OrderBuilder().Build();

        order.OrderItems.ShouldBeAssignableTo<IReadOnlyCollection<OrderItem>>();
    }

    [Fact]
    public void OrderItem_UnitPriceCurrency_MatchesSnapshotCurrency()
    {
        var snapshot = new OrderItemSnapshotBuilder()
            .WithUnitPrice(Money.Create(1_000m, "IRT"))
            .Build();

        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();
        var sut = order.OrderItems.Single();

        sut.UnitPrice.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void OrderItem_QuantityMatchesSnapshotQuantity()
    {
        var snapshot = new OrderItemSnapshotBuilder().WithQuantity(7).Build();

        var order = new OrderBuilder().WithItemSnapshots(snapshot).Build();

        order.OrderItems.Single().Quantity.ShouldBe(7);
    }
}
