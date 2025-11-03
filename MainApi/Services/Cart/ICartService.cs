namespace MainApi.Services.Cart;

public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(int userId);
    Task<CartDto?> CreateCartAsync(int userId);
    Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int userId, AddToCartDto dto);
    Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto);
    Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int userId, int itemId);
    Task<bool> ClearCartAsync(int userId);
    Task<int> GetCartItemsCountAsync(int userId);
}