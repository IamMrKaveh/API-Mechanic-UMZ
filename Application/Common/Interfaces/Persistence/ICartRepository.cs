namespace Application.Common.Interfaces.Persistence;

public interface ICartRepository
{
    Task<Domain.Cart.Cart?> GetCartAsync(int? userId, string? guestId = null);

    Task<Domain.Cart.Cart?> GetCartEntityAsync(int? userId, string? guestId = null);

    Task AddCartAsync(Domain.Cart.Cart cart);

    Task AddCartItemAsync(Domain.Cart.CartItem item);

    Task<Domain.Product.ProductVariant?> GetVariantByIdAsync(int variantId);

    Task<Domain.Cart.CartItem?> GetCartItemAsync(int cartId, int variantId);

    void SetCartItemRowVersion(Domain.Cart.CartItem item, byte[] rowVersion);

    Task<Domain.Cart.CartItem?> GetCartItemWithDetailsAsync(int itemId, int? userId, string? guestId);

    void RemoveCartItem(Domain.Cart.CartItem item);

    void RemoveCartItems(IEnumerable<Domain.Cart.CartItem> items);

    void RemoveCart(Domain.Cart.Cart cart);

    Task<int> GetCartItemsCountAsync(int? userId, string? guestId);

    void UpdateCartItem(Domain.Cart.CartItem item);

    Task<bool> UserExistsAsync(int userId);
}