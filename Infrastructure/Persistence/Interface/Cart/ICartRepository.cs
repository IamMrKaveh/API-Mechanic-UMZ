namespace Infrastructure.Persistence.Interface.Cart;

public interface ICartRepository
{
    Task<Domain.Cart.Cart?> GetCartAsync(int? userId, string? guestId = null);
    Task<Domain.Cart.Cart?> GetCartEntityAsync(int? userId, string? guestId = null);
    Task<Domain.Cart.Cart?> GetByUserIdAsync(int userId);
    Task<List<CartItem>> GetCartItemsByUserIdAsync(int userId);
    Task AddCartAsync(Domain.Cart.Cart cart);
    Task AddCartItemAsync(CartItem item);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    Task<CartItem?> GetCartItemAsync(int cartId, int variantId);
    void SetCartItemRowVersion(CartItem item, byte[] rowVersion);
    Task<CartItem?> GetCartItemWithDetailsAsync(int itemId, int? userId, string? guestId);
    void RemoveCartItem(CartItem item);
    void RemoveCartItems(IEnumerable<CartItem> items);
    void RemoveCart(Domain.Cart.Cart cart);
    Task<int> GetCartItemsCountAsync(int? userId, string? guestId);
    void UpdateCartItem(CartItem item);
    Task<bool> UserExistsAsync(int userId);
    Task ClearCartAsync(int userId);
}