using Domain.Wishlist.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wishlist.Repositories;

public class WishlistRepository(DBContext context) : IWishlistRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        Domain.Wishlist.Aggregates.Wishlist wishlist,
        CancellationToken ct = default)
    {
        await _context.Wishlists.AddAsync(wishlist, ct);
    }

    public async Task RemoveAsync(
        int userId,
        int productId,
        CancellationToken ct = default)
    {
        var entry = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

        if (entry != null)
            _context.Wishlists.Remove(entry);
    }

    public async Task<bool> ExistsAsync(
        int userId,
        int productId,
        CancellationToken ct = default)
    {
        return await _context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct);
    }
}