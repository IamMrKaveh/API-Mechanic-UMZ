namespace Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    private readonly LedkaContext _context;

    public CartRepository(LedkaContext context)
    {
        _context = context;
    }

    private IQueryable<Domain.Cart.Cart> GetFullCartQuery()
    {
        return _context.Set<Domain.Cart.Cart>()
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.Images)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Variant)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.AttributeType);
    }

    public async Task<Domain.Cart.Cart?> GetCartAsync(int? userId, string? guestId = null)
    {
        return await GetFullCartQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));
    }

    public async Task<Domain.Cart.Cart?> GetCartEntityAsync(int? userId, string? guestId)
    {
        return await _context.Set<Domain.Cart.Cart>()
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));
    }

    public async Task AddCartAsync(Domain.Cart.Cart cart)
    {
        await _context.Set<Domain.Cart.Cart>().AddAsync(cart);
    }

    public async Task AddCartItemAsync(Domain.Cart.CartItem item)
    {
        await _context.Set<Domain.Cart.CartItem>().AddAsync(item);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.Set<Domain.Product.ProductVariant>()
        .Include(v => v.InventoryTransactions)
        .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public async Task<Domain.Cart.CartItem?> GetCartItemAsync(int cartId, int variantId)
    {
        return await _context.Set<Domain.Cart.CartItem>()
            .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.VariantId == variantId);
    }

    public void SetCartItemRowVersion(Domain.Cart.CartItem item, byte[] rowVersion)
    {
        _context.Entry(item).Property("RowVersion").OriginalValue = rowVersion;
    }

    public async Task<Domain.Cart.CartItem?> GetCartItemWithDetailsAsync(int itemId, int? userId, string? guestId)
    {
        return await _context.Set<Domain.Cart.CartItem>()
            .Include(ci => ci.Variant)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ((userId.HasValue && ci.Cart!.UserId == userId) || (!string.IsNullOrEmpty(guestId) && ci.Cart!.GuestToken == guestId)));
    }

    public void RemoveCartItem(Domain.Cart.CartItem item)
    {
        _context.Set<Domain.Cart.CartItem>().Remove(item);
    }

    public void RemoveCartItems(IEnumerable<Domain.Cart.CartItem> items)
    {
        _context.Set<Domain.Cart.CartItem>().RemoveRange(items);
    }

    public void RemoveCart(Domain.Cart.Cart cart)
    {
        _context.Set<Domain.Cart.Cart>().Remove(cart);
    }

    public async Task<int> GetCartItemsCountAsync(int? userId, string? guestId)
    {
        var cart = await _context.Set<Domain.Cart.Cart>()
            .Include(c => c.CartItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));
        return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
    }

    public void UpdateCartItem(Domain.Cart.CartItem item)
    {
        _context.Set<Domain.Cart.CartItem>().Update(item);
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted);
    }
}