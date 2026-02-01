namespace Infrastructure.Persistence.Interface.User;

public interface IWishlistRepository
{
    Task<List<Wishlist>> GetByUserIdAsync(int userId);
    Task<Wishlist?> GetByProductAsync(int userId, int productId);
    Task AddAsync(Wishlist wishlist);
    void Remove(Wishlist wishlist);
    Task<bool> ExistsAsync(int userId, int productId);
}