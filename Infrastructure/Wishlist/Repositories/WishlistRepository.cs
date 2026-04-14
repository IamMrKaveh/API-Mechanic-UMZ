using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Domain.Wishlist.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wishlist.Repositories;

public sealed class WishlistRepository(DBContext context) : IWishlistRepository
{
    public async Task AddAsync(Domain.Wishlist.Aggregates.Wishlist wishlist, CancellationToken ct = default)
        => await context.Wishlists.AddAsync(wishlist, ct);

    public void Update(Domain.Wishlist.Aggregates.Wishlist wishlist)
        => context.Wishlists.Update(wishlist);

    public async Task RemoveAsync(WishlistId id, CancellationToken ct = default)
    {
        var entity = await context.Wishlists.FindAsync([id], ct);
        if (entity is not null)
            context.Wishlists.Remove(entity);
    }

    public async Task<Domain.Wishlist.Aggregates.Wishlist?> GetByIdAsync(WishlistId id, CancellationToken ct = default)
        => await context.Wishlists.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Domain.Wishlist.Aggregates.Wishlist?> GetByUserAndProductAsync(
        UserId userId, ProductId productId, CancellationToken ct = default)
        => await context.Wishlists.FirstOrDefaultAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);

    public async Task<IReadOnlyList<Domain.Wishlist.Aggregates.Wishlist>> GetByUserIdAsync(
        UserId userId, CancellationToken ct = default)
    {
        var result = await context.Wishlists
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<Domain.Wishlist.Aggregates.Wishlist?> GetByProductIdAsync(
        ProductId productId, CancellationToken ct = default)
        => await context.Wishlists.FirstOrDefaultAsync(w => w.ProductId == productId, ct);

    public async Task<int> CountByUserIdAsync(UserId userId, CancellationToken ct = default)
        => await context.Wishlists.CountAsync(w => w.UserId == userId, ct);
}