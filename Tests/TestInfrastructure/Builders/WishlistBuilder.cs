using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using WishlistAggregate = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.TestInfrastructure.Builders;

public sealed class WishlistBuilder
{
    private UserId _userId = UserId.NewId();
    private ProductId _productId = ProductId.NewId();

    public WishlistBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public WishlistBuilder WithProductId(ProductId productId)
    {
        _productId = productId;
        return this;
    }

    public WishlistAggregate Build() => WishlistAggregate.Create(_userId, _productId);
}
