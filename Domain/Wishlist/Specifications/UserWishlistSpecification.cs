using Domain.User.ValueObjects;

namespace Domain.Wishlist.Specifications;

public sealed class UserWishlistSpecification : Specification<Aggregates.Wishlist>
{
    private readonly UserId _userId;

    public UserWishlistSpecification(UserId userId)
    {
        Guard.Against.Null(userId, nameof(userId));
        _userId = userId;
    }

    public override Expression<Func<Aggregates.Wishlist, bool>> ToExpression()
    {
        return w => w.UserId == _userId;
    }
}