using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Events;
using Domain.Wishlist.ValueObjects;

namespace Tests.Domain.Wishlist.Events;

public class WishlistEventsTests
{
    [Fact]
    public void WishlistItemAddedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var wishlistId = WishlistId.NewId();
        var userId = UserId.NewId();
        var productId = ProductId.NewId();

        var sut = new WishlistItemAddedEvent(wishlistId, userId, productId);

        sut.WishlistId.ShouldBe(wishlistId);
        sut.UserId.ShouldBe(userId);
        sut.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void WishlistItemAddedEvent_HasUniqueEventId()
    {
        var first = new WishlistItemAddedEvent(WishlistId.NewId(), UserId.NewId(), ProductId.NewId());
        var second = new WishlistItemAddedEvent(WishlistId.NewId(), UserId.NewId(), ProductId.NewId());

        first.EventId.ShouldNotBe(second.EventId);
    }
}
