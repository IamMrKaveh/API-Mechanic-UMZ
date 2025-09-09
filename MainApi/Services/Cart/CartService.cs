namespace MainApi.Services.Cart;

public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(int userId);
    Task<bool> AddItemToCartAsync(int userId, AddToCartDto dto);
    Task<bool> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto);
    Task<bool> RemoveItemFromCartAsync(int userId, int itemId);
    Task<bool> ClearCartAsync(int userId);
    Task<int> GetCartItemsCountAsync(int userId);
}

public class CartService : ICartService
{
    private readonly MechanicContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(MechanicContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return new CartDto
                {
                    UserId = userId,
                    CartItems = new List<CartItemDto>(),
                    TotalItems = 0,
                    TotalPrice = 0
                };
            }

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CartItems = cart.CartItems?.Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product?.Name ?? "",
                    SellingPrice = ci.Product?.SellingPrice ?? 0,
                    Quantity = ci.Quantity,
                    TotalPrice = (ci.Product?.SellingPrice ?? 0) * ci.Quantity
                }).ToList() ?? new List<CartItemDto>(),
                TotalItems = cart.TotalItems,
                TotalPrice = cart.TotalPrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> AddItemToCartAsync(int userId, AddToCartDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (dto.Quantity <= 0 || dto.Quantity > 1000)
            {
                _logger.LogWarning("Invalid quantity for cart item: {Quantity}", dto.Quantity);
                return false;
            }

            var cart = await GetOrCreateCartAsync(userId);
            var product = await _context.TProducts.FindAsync(dto.ProductId);

            if (product == null)
            {
                _logger.LogWarning("Product not found or inactive: {ProductId}", dto.ProductId);
                return false;
            }

            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            var requiredStock = existingItem != null ? existingItem.Quantity + dto.Quantity : dto.Quantity;

            if (!product.IsUnlimited && product.Count < requiredStock)
            {
                _logger.LogWarning("Insufficient stock for ProductId {ProductId}. Available: {Available}, Required: {Required}",
                    dto.ProductId, product.Count, requiredStock);
                return false;
            }

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                cart.CartItems.Add(new TCartItems { ProductId = dto.ProductId, Quantity = dto.Quantity, CartId = cart.Id });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict on AddItemToCart for ProductId {ProductId}", dto.ProductId);
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding item to cart: ProductId {ProductId}", dto.ProductId);
            return false;
        }
    }

    public async Task<bool> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (dto.Quantity <= 0 || dto.Quantity > 1000)
            {
                _logger.LogWarning("Invalid quantity for update: {Quantity}", dto.Quantity);
                return false;
            }
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);

            if (cartItem == null) return false;

            var product = cartItem.Product;
            if (product == null) return false;

            if (!product.IsUnlimited && product.Count < dto.Quantity)
            {
                return false;
            }

            cartItem.Quantity = dto.Quantity;
            _context.TCartItems.Update(cartItem);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning("Concurrency conflict on UpdateCartItem for ItemId {ItemId}", itemId);
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating cart item: ItemId {ItemId}", itemId);
            return false;
        }
    }

    private async Task<TCarts> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new TCarts { UserId = userId };
            _context.TCarts.Add(cart);
            await _context.SaveChangesAsync();
        }
        return cart;
    }

    public async Task<bool> RemoveItemFromCartAsync(int userId, int itemId)
    {
        try
        {
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);

            if (cartItem == null) return false;

            _context.TCartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item: ItemId {ItemId}", itemId);
            return false;
        }
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any()) return true;

            _context.TCartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetCartItemsCountAsync(int userId)
    {
        try
        {
            return await _context.TCarts
                .Where(c => c.UserId == userId)
                .Select(c => c.TotalItems)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart items count for user: {UserId}", userId);
            return 0;
        }
    }
}