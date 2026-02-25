namespace Infrastructure.Cart.Repositories;

public class CartRepository : ICartRepository
{
    private readonly Persistence.Context.DBContext _context;

    public CartRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<Domain.Cart.Cart?> GetByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted, ct);
    }

    public async Task<Domain.Cart.Cart?> GetByGuestTokenAsync(string guestToken, CancellationToken ct = default)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.GuestToken == guestToken && !c.IsDeleted, ct);
    }

    public async Task<Domain.Cart.Cart?> GetCartAsync(int? userId, string? guestToken, CancellationToken ct = default)
    {
        if (userId.HasValue)
            return await GetByUserIdAsync(userId.Value, ct);

        if (!string.IsNullOrEmpty(guestToken))
            return await GetByGuestTokenAsync(guestToken, ct);

        return null;
    }

    public async Task AddAsync(Domain.Cart.Cart cart, CancellationToken ct = default)
    {
        await _context.Carts.AddAsync(cart, ct);
    }

    public void Delete(Domain.Cart.Cart cart)
    {
        _context.Carts.Remove(cart);
    }

    public async Task<bool> ExistsForUserAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Carts
            .AnyAsync(c => c.UserId == userId && !c.IsDeleted, ct);
    }

    public async Task<bool> ExistsForGuestAsync(string guestToken, CancellationToken ct = default)
    {
        return await _context.Carts
            .AnyAsync(c => c.GuestToken == guestToken && !c.IsDeleted, ct);
    }

    public async Task<int> DeleteExpiredGuestCartsAsync(DateTime olderThan, CancellationToken ct = default)
    {
        var expired = await _context.Carts
            .Where(c => c.GuestToken != null && c.LastUpdated < olderThan && !c.IsDeleted)
            .ToListAsync(ct);

        _context.Carts.RemoveRange(expired);
        return expired.Count;
    }

    public async Task<int> GetItemsCountAsync(int? userId, string? guestToken, CancellationToken ct = default)
    {
        var cart = await GetCartAsync(userId, guestToken, ct);
        return cart?.TotalItems ?? 0;
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public async Task ClearCartAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetByUserIdAsync(userId, ct);
        if (cart is null) return;

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync(ct);
    }
}