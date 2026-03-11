namespace Tests.DomainTest.Cart;

public class CartTotalTests
{
    [Fact]
    public void TotalPrice_WithSingleItem_ShouldCalculateCorrectly()
    {
        var cart = new CartBuilder().WithItem(1, 3, 50_000m).Build();

        cart.TotalPrice.Should().Be(150_000m);
    }

    [Fact]
    public void TotalPrice_WithMultipleItems_ShouldSumAllItems()
    {
        var cart = new CartBuilder()
            .WithItem(1, 2, 50_000m)
            .WithItem(2, 1, 80_000m)
            .Build();

        cart.TotalPrice.Should().Be(180_000m);
    }

    [Fact]
    public void TotalPrice_WhenCartEmpty_ShouldBeZero()
    {
        var cart = new CartBuilder().Build();

        cart.TotalPrice.Should().Be(0);
    }

    [Fact]
    public void TotalItems_ShouldSumQuantitiesAcrossItems()
    {
        var cart = new CartBuilder()
            .WithItem(1, 3, 50_000m)
            .WithItem(2, 2, 20_000m)
            .Build();

        cart.TotalItems.Should().Be(5);
    }

    [Fact]
    public void TotalPrice_AfterUpdatingQuantity_ShouldRecalculate()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.UpdateItemQuantity(1, 4);

        cart.TotalPrice.Should().Be(200_000m);
    }

    [Fact]
    public void TotalPrice_AfterUpdatingPrice_ShouldRecalculate()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.UpdateItemPrice(1, 70_000m);

        cart.TotalPrice.Should().Be(140_000m);
    }

    [Fact]
    public void TotalPrice_AfterRemovingItem_ShouldExcludeRemovedItem()
    {
        var cart = new CartBuilder()
            .WithItem(1, 2, 50_000m)
            .WithItem(2, 1, 80_000m)
            .Build();

        cart.RemoveItem(1);

        cart.TotalPrice.Should().Be(80_000m);
    }

    [Fact]
    public void TotalPrice_AfterClear_ShouldBeZero()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.Clear();

        cart.TotalPrice.Should().Be(0);
    }

    [Fact]
    public void FindItemByVariant_WhenExists_ShouldReturnItem()
    {
        var cart = new CartBuilder().WithItem(5, 3, 100_000m).Build();

        var item = cart.FindItemByVariant(5);

        item.Should().NotBeNull();
        item!.Quantity.Should().Be(3);
        item.SellingPrice.Should().Be(100_000m);
    }

    [Fact]
    public void FindItemByVariant_WhenNotExists_ShouldReturnNull()
    {
        var cart = new CartBuilder().Build();

        var result = cart.FindItemByVariant(999);

        result.Should().BeNull();
    }
}