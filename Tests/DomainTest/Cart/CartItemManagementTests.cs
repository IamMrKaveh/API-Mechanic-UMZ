namespace Tests.DomainTest.Cart;

public class CartItemManagementTests
{
    [Fact]
    public void AddItem_WithValidData_ShouldAddItemToCart()
    {
        var cart = new CartBuilder().Build();

        cart.AddItem(variantId: 1, quantity: 2, unitPrice: 50_000m);

        cart.HasItems.Should().BeTrue();
        cart.CartItems.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_ShouldRaiseCartItemAddedEvent()
    {
        var cart = new CartBuilder().Build();
        cart.ClearDomainEvents();

        cart.AddItem(1, 1, 100_000m);

        cart.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CartItemAddedEvent");
    }

    [Fact]
    public void AddItem_SamVariantTwice_ShouldUpdateQuantity()
    {
        var cart = new CartBuilder().Build();
        cart.AddItem(variantId: 1, quantity: 2, unitPrice: 50_000m);

        cart.AddItem(variantId: 1, quantity: 3, unitPrice: 50_000m);

        cart.CartItems.Should().HaveCount(1);
        cart.GetItemQuantity(1).Should().Be(5);
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldThrowDomainException()
    {
        var cart = new CartBuilder().Build();

        var act = () => cart.AddItem(1, 0, 50_000m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrowDomainException()
    {
        var cart = new CartBuilder().Build();

        var act = () => cart.AddItem(1, -1, 50_000m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_WithNegativePrice_ShouldThrowDomainException()
    {
        var cart = new CartBuilder().Build();

        var act = () => cart.AddItem(1, 1, -100m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveItem_WhenItemExists_ShouldRemoveFromCart()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.RemoveItem(variantId: 1);

        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void RemoveItem_ShouldRaiseCartItemRemovedEvent()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();
        cart.ClearDomainEvents();

        cart.RemoveItem(1);

        cart.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CartItemRemovedEvent");
    }

    [Fact]
    public void RemoveItem_WhenItemNotExists_ShouldThrowException()
    {
        var cart = new CartBuilder().Build();

        var act = () => cart.RemoveItem(999);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void UpdateItemQuantity_WithValidQuantity_ShouldUpdateQuantity()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.UpdateItemQuantity(variantId: 1, newQuantity: 5);

        cart.GetItemQuantity(1).Should().Be(5);
    }

    [Fact]
    public void UpdateItemQuantity_WithZeroOrNegative_ShouldRemoveItem()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.UpdateItemQuantity(variantId: 1, newQuantity: 0);

        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void UpdateItemQuantity_ShouldRaiseCartItemUpdatedEvent()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();
        cart.ClearDomainEvents();

        cart.UpdateItemQuantity(1, 10);

        cart.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CartItemUpdatedEvent");
    }

    [Fact]
    public void Clear_WhenCartHasItems_ShouldRemoveAllItems()
    {
        var cart = new CartBuilder()
            .WithItem(1, 2, 50_000m)
            .WithItem(2, 1, 30_000m)
            .Build();

        cart.Clear();

        cart.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRaiseCartClearedEvent()
    {
        var cart = new CartBuilder().WithItem(1, 1, 50_000m).Build();
        cart.ClearDomainEvents();

        cart.Clear();

        cart.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CartClearedEvent");
    }

    [Fact]
    public void Clear_WhenAlreadyEmpty_ShouldNotRaiseEvent()
    {
        var cart = new CartBuilder().Build();
        cart.ClearDomainEvents();

        cart.Clear();

        cart.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateItemPrice_ShouldChangeItemPrice()
    {
        var cart = new CartBuilder().WithItem(1, 2, 50_000m).Build();

        cart.UpdateItemPrice(variantId: 1, newPrice: 60_000m);

        cart.CartItems.First().SellingPrice.Should().Be(60_000m);
    }

    [Fact]
    public void ContainsVariant_WhenVariantInCart_ShouldReturnTrue()
    {
        var cart = new CartBuilder().WithItem(5, 1, 10_000m).Build();

        cart.ContainsVariant(5).Should().BeTrue();
    }

    [Fact]
    public void ContainsVariant_WhenVariantNotInCart_ShouldReturnFalse()
    {
        var cart = new CartBuilder().Build();

        cart.ContainsVariant(999).Should().BeFalse();
    }

    [Fact]
    public void GetVariantIds_ShouldReturnAllVariantIds()
    {
        var cart = new CartBuilder()
            .WithItem(1, 1, 10_000m)
            .WithItem(2, 2, 20_000m)
            .Build();

        var ids = cart.GetVariantIds().ToList();

        ids.Should().Contain(1);
        ids.Should().Contain(2);
        ids.Should().HaveCount(2);
    }
}