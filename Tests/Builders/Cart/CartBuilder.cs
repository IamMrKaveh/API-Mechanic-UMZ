namespace Tests.Builders.Cart;

public class CartBuilder
{
    private int? _userId = 1;
    private string? _guestToken = null;

    public CartBuilder ForUser(int userId)
    {
        _userId = userId;
        _guestToken = null;
        return this;
    }

    public CartBuilder ForGuest(string guestToken = "guest-token-abc123")
    {
        _userId = null;
        _guestToken = guestToken;
        return this;
    }

    public CartBuilder WithItem(int variantId, int quantity, decimal price)
    {
        _pendingItems.Add((variantId, quantity, price));
        return this;
    }

    private readonly List<(int VariantId, int Quantity, decimal Price)> _pendingItems = new();

    public Domain.Cart.Aggregates.Cart Build()
    {
        var cart = _userId.HasValue
            ? Domain.Cart.Aggregates.Cart.CreateForUser(_userId.Value)
            : Domain.Cart.Aggregates.Cart.CreateForGuest(_guestToken!);

        foreach (var (variantId, quantity, price) in _pendingItems)
            cart.AddItem(variantId, quantity, price);

        return cart;
    }
}