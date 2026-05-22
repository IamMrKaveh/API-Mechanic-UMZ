using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Wishlist.Interfaces;

public interface IWishlistRepository
{
    Task AddAsync(
        Aggregates.Wishlist wishlist,
        CancellationToken ct = default);

    Task RemoveAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);

    Task<Aggregates.Wishlist?> GetByUserAndProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);
}