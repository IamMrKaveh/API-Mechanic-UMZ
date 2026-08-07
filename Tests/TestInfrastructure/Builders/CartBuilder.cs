using Domain.Cart.Aggregates;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class CartBuilder
{
    private bool _asUser = true;
    private UserId _userId = UserId.NewId();
    private GuestToken _guestToken = GuestToken.Generate();

    public CartBuilder ForUser(UserId userId)
    {
        _asUser = true;
        _userId = userId;
        return this;
    }

    public CartBuilder ForGuest(GuestToken guestToken)
    {
        _asUser = false;
        _guestToken = guestToken;
        return this;
    }

    public Cart Build() =>
        _asUser
            ? Cart.CreateForUser(_userId)
            : Cart.CreateForGuest(_guestToken);
}
