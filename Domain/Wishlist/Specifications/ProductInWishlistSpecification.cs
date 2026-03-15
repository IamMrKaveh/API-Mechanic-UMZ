using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Wishlist.Specifications;

public sealed class ProductInWishlistSpecification : Specification<Aggregates.Wishlist>
{
    private readonly UserId _userId;
    private readonly ProductId _productId;

    public ProductInWishlistSpecification(UserId userId, ProductId productId)
    {
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(productId, nameof(productId));
        _userId = userId;
        _productId = productId;
    }

    public override Expression<Func<Aggregates.Wishlist, bool>> ToExpression()
    {
        return w => w.UserId == _userId && w.ProductId == _productId;
    }
}