using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Domain.Wishlist.Interfaces;

public interface IWishlistRepository
{
    Task AddAsync(
        Aggregates.Wishlist wishlist,
        CancellationToken ct = default);

    Task RemoveAsync(
        WishlistId wishlistId,
        CancellationToken ct = default);

    Task RemoveAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);

    Task<Aggregates.Wishlist?> GetByIdAsync(
        WishlistId wishlistId,
        CancellationToken ct = default);

    Task<Aggregates.Wishlist?> GetByUserAndProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Wishlist>> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Wishlist>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);

    Task<int> CountByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);
}