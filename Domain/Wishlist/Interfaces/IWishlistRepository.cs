namespace Domain.Wishlist.Interfaces;

public interface IWishlistRepository
{
    Task AddAsync(
        Aggregates.Wishlist wishlist,
        CancellationToken ct = default);

    Task RemoveAsync(
        int userId,
        int productId,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        int userId,
        int productId,
        CancellationToken ct = default);
}