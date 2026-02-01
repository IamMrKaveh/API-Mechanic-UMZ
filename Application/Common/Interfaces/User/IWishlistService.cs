namespace Application.Common.Interfaces.User;

public interface IWishlistService
{
    Task<ServiceResult<List<WishlistDto>>> GetUserWishlistAsync(int userId);
    Task<ServiceResult> ToggleWishlistAsync(int userId, int productId);
    Task<ServiceResult<bool>> IsInWishlistAsync(int userId, int productId);
}