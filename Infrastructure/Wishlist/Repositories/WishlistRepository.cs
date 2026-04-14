using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wishlist.Repositories;

public sealed class WishlistRepository(DBContext context) : IWishlistRepository
{
    public async Task<Domain.Wishlist.Aggregates.Wishlist?> GetByUserAndProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
    {
        return await context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);
    }

    public async Task<bool> ExistsAsync(UserId userId, ProductId productId, CancellationToken ct = default)
    {
        return await context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct);
    }

    public async Task<IReadOnlyList<Domain.Wishlist.Aggregates.Wishlist>> GetByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var results = await context.Wishlists
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task AddAsync(Domain.Wishlist.Aggregates.Wishlist wishlist, CancellationToken ct = default)
    {
        await context.Wishlists.AddAsync(wishlist, ct);
    }

    public async Task RemoveAsync(UserId userId, ProductId productId, CancellationToken ct = default)
    {
        var wishlist = await context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

        if (wishlist is not null)
            context.Wishlists.Remove(wishlist);
    }
}