namespace MainApi.Services.Cart;

public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(int userId);
    Task<CartDto?> CreateCartAsync(int userId);
    Task<CartOperationResult> AddItemToCartAsync(int userId, AddToCartDto dto);
    Task<bool> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto);
    Task<bool> RemoveItemFromCartAsync(int userId, int itemId);
    Task<bool> ClearCartAsync(int userId);
    Task<int> GetCartItemsCountAsync(int userId);
    Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15);
}
