namespace Tests.DomainTest.Cart;

public class CartMergeTests
{
    [Fact]
    public void MergeWith_WhenSourceCartHasItems_ShouldAddItemsToTarget()
    {
        var userCart = new CartBuilder().ForUser(1).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 2, 50_000m).Build();

        userCart.MergeWith(guestCart);

        userCart.ContainsVariant(1).Should().BeTrue();
    }

    [Fact]
    public void MergeWith_ShouldRaiseCartMergedEvent()
    {
        var userCart = new CartBuilder().ForUser(1).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 2, 50_000m).Build();
        userCart.ClearDomainEvents();

        userCart.MergeWith(guestCart);

        userCart.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CartMergedEvent");
    }

    [Fact]
    public void MergeWith_WhenSourceCartIsEmpty_ShouldNotChangeTarget()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 2, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().Build();
        userCart.ClearDomainEvents();

        userCart.MergeWith(guestCart);

        userCart.CartItems.Should().HaveCount(1);
        userCart.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MergeWith_WithKeepHigherQuantityStrategy_ShouldKeepHigherQuantity()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 3, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 5, 50_000m).Build();

        userCart.MergeWith(guestCart, CartMergeStrategy.KeepHigherQuantity);

        userCart.GetItemQuantity(1).Should().Be(5);
    }

    [Fact]
    public void MergeWith_WithSumQuantitiesStrategy_ShouldSumQuantities()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 3, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 4, 50_000m).Build();

        userCart.MergeWith(guestCart, CartMergeStrategy.SumQuantities);

        userCart.GetItemQuantity(1).Should().Be(7);
    }

    [Fact]
    public void MergeWith_WithKeepUserCartStrategy_ShouldKeepUserQuantity()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 3, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 8, 50_000m).Build();

        userCart.MergeWith(guestCart, CartMergeStrategy.KeepUserCart);

        userCart.GetItemQuantity(1).Should().Be(3);
    }

    [Fact]
    public void MergeWith_WithKeepGuestCartStrategy_ShouldKeepGuestQuantity()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 3, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 8, 50_000m).Build();

        userCart.MergeWith(guestCart, CartMergeStrategy.KeepGuestCart);

        userCart.GetItemQuantity(1).Should().Be(8);
    }

    [Fact]
    public void MergeWith_WithNewItemInGuestCart_ShouldAddItemToUserCart()
    {
        var userCart = new CartBuilder().ForUser(1).WithItem(1, 2, 50_000m).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(2, 1, 30_000m).Build();

        userCart.MergeWith(guestCart);

        userCart.CartItems.Should().HaveCount(2);
        userCart.ContainsVariant(2).Should().BeTrue();
    }

    [Fact]
    public void MergeWith_WithSelf_ShouldThrowException()
    {
        var cart = new CartBuilder().ForUser(1).Build();

        var act = () => cart.MergeWith(cart);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void MergeWith_WhenSourceCartIsDeleted_ShouldThrowException()
    {
        var userCart = new CartBuilder().ForUser(1).Build();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 2, 50_000m).Build();
        guestCart.Delete();

        var act = () => userCart.MergeWith(guestCart);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void MergeWith_WhenTargetCartIsDeleted_ShouldThrowException()
    {
        var userCart = new CartBuilder().ForUser(1).Build();
        userCart.Delete();
        var guestCart = new CartBuilder().ForGuest().WithItem(1, 2, 50_000m).Build();

        var act = () => userCart.MergeWith(guestCart);

        act.Should().Throw<Exception>();
    }
}