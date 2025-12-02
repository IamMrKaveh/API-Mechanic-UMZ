namespace Application.Common.Interfaces;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(int? userId, string? guestId = null);

    Task<CartDto?> GetCartByUserIdAsync(int userId);

    Task<CartDto?> CreateCartAsync(int userId);

    Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int? userId, string? guestId, AddToCartDto dto);

    Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int userId, AddToCartDto dto);

    Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int? userId, string? guestId, int itemId, UpdateCartItemDto dto);

    Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto);

    Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int? userId, string? guestId, int itemId);

    Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int userId, int itemId);

    Task<bool> ClearCartAsync(int? userId, string? guestId);

    Task<bool> ClearCartAsync(int userId);

    Task<int> GetCartItemsCountAsync(int? userId, string? guestId = null);

    Task<int> GetCartItemsCountAsync(int userId);

    Task<Domain.Cart.Cart?> GetCartEntityAsync(int? userId, string? guestId);

    Task MergeCartAsync(int userId, string guestId);
}