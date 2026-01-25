namespace Application.Common.Interfaces.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetCartAsync(int? userId, string? guestId = null);
    Task<Cart?> GetCartEntityAsync(int? userId, string? guestId = null);
    Task<Cart?> GetByUserIdAsync(int userId);
    Task<List<CartItem>> GetCartItemsByUserIdAsync(int userId);
    Task AddCartAsync(Cart cart);
    Task AddCartItemAsync(CartItem item);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    Task<CartItem?> GetCartItemAsync(int cartId, int variantId);
    void SetCartItemRowVersion(CartItem item, byte[] rowVersion);
    Task<CartItem?> GetCartItemWithDetailsAsync(int itemId, int? userId, string? guestId);
    void RemoveCartItem(CartItem item);
    void RemoveCartItems(IEnumerable<CartItem> items);
    void RemoveCart(Cart cart);
    Task<int> GetCartItemsCountAsync(int? userId, string? guestId);
    void UpdateCartItem(CartItem item);
    Task<bool> UserExistsAsync(int userId);
    Task ClearCartAsync(int userId);
}