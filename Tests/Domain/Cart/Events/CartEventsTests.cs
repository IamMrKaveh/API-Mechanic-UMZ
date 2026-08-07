using Domain.Cart.Events;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Cart.Events;

public class CartEventsTests
{
    [Fact]
    public void CartCreatedEvent_ForUserCart_ExposesConstructorArgumentsAsProperties()
    {
        var cartId = CartId.NewId();
        var userId = UserId.NewId();

        var sut = new CartCreatedEvent(cartId, userId, null);

        sut.CartId.ShouldBe(cartId);
        sut.UserId.ShouldBe(userId);
        sut.GuestToken.ShouldBeNull();
    }

    [Fact]
    public void CartCreatedEvent_ForGuestCart_ExposesGuestTokenAndNullUserId()
    {
        var cartId = CartId.NewId();
        var token = GuestToken.Generate();

        var sut = new CartCreatedEvent(cartId, null, token);

        sut.CartId.ShouldBe(cartId);
        sut.UserId.ShouldBeNull();
        sut.GuestToken.ShouldBe(token);
    }

    [Fact]
    public void CartItemAddedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var cartId = CartId.NewId();
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();
        var productName = ProductName.Create("Product X");

        var sut = new CartItemAddedEvent(cartId, variantId, productId, productName, 3, 100m);

        sut.CartId.ShouldBe(cartId);
        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
        sut.ProductName.ShouldBe(productName);
        sut.Quantity.ShouldBe(3);
        sut.UnitPrice.ShouldBe(100m);
    }

    [Fact]
    public void CartItemRemovedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var cartId = CartId.NewId();
        var variantId = VariantId.NewId();

        var sut = new CartItemRemovedEvent(cartId, variantId, 5);

        sut.CartId.ShouldBe(cartId);
        sut.VariantId.ShouldBe(variantId);
        sut.RemovedQuantity.ShouldBe(5);
    }

    [Fact]
    public void CartCheckedOutEvent_ExposesConstructorArgumentsAsProperties()
    {
        var cartId = CartId.NewId();
        var userId = UserId.NewId();

        var sut = new CartCheckedOutEvent(cartId, userId, 4, 500m);

        sut.CartId.ShouldBe(cartId);
        sut.UserId.ShouldBe(userId);
        sut.ItemCount.ShouldBe(4);
        sut.TotalAmount.ShouldBe(500m);
    }

    [Fact]
    public void CartCheckedOutEvent_WithNullUserId_StoresNull()
    {
        var sut = new CartCheckedOutEvent(CartId.NewId(), null, 1, 10m);

        sut.UserId.ShouldBeNull();
    }

    [Fact]
    public void CartMergedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var targetCartId = CartId.NewId();
        var sourceCartId = CartId.NewId();
        var userId = UserId.NewId();

        var sut = new CartMergedEvent(targetCartId, sourceCartId, userId, 7);

        sut.TargetCartId.ShouldBe(targetCartId);
        sut.SourceCartId.ShouldBe(sourceCartId);
        sut.UserId.ShouldBe(userId);
        sut.MergedItemCount.ShouldBe(7);
    }
}
