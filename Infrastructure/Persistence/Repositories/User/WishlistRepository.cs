namespace Infrastructure.Persistence.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly LedkaContext _context;

    public WishlistRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<List<Wishlist>> GetByUserIdAsync(int userId)
    {
        return await _context.Wishlists
            .Include(w => w.Product)
            .ThenInclude(p => p.Images)
            .Where(w => w.UserId == userId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<Wishlist?> GetByProductAsync(int userId, int productId)
    {
        return await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task AddAsync(Wishlist wishlist)
    {
        await _context.Wishlists.AddAsync(wishlist);
    }

    public void Remove(Wishlist wishlist)
    {
        _context.Wishlists.Remove(wishlist);
    }

    public async Task<bool> ExistsAsync(int userId, int productId)
    {
        return await _context.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId && !w.IsDeleted);
    }
}