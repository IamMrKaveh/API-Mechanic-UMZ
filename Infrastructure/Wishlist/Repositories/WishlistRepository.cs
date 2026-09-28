using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Infrastructure.Wishlist.Repositories;

public sealed class WishlistRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Domain.Wishlist.Aggregates.Wishlist, Domain.Wishlist.ValueObjects.WishlistId>(context), IWishlistRepository
{
    public async Task<Domain.Wishlist.Aggregates.Wishlist?> GetByUserAndProductAsync(
        UserId userId, ProductId productId, CancellationToken ct = default)
        => await Context.Wishlists.FirstOrDefaultAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);

    public async Task RemoveAsync(UserId userId, ProductId productId, CancellationToken ct = default)
    {
        var wishlist = await Context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

        if (wishlist is not null)
            Context.Wishlists.Remove(wishlist);
    }

    public async Task ClearAsync(UserId userId, CancellationToken ct = default)
    {
        var items = await Context.Wishlists
            .Where(w => w.UserId == userId)
            .ToListAsync(ct);

        if (items.Count > 0)
            Context.Wishlists.RemoveRange(items);
    }

    public async Task<bool> ExistsAsync(UserId userId, ProductId productId, CancellationToken ct = default)
        => await Context.Wishlists.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);
}