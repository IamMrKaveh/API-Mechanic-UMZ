namespace MainApi.Services.Cart;

public enum CartOperationResult
{
    Success,
    NotFound,
    OutOfStock,
    OptionsRequired,
    Error
}

public class CartService : ICartService
{
    private readonly MechanicContext _context;
    private readonly ILogger<CartService> _logger;
    private readonly IConnectionMultiplexer _redis;

    public CartService(MechanicContext context, ILogger<CartService> logger, IConnectionMultiplexer redis)
    {
        _context = context;
        _logger = logger;
        _redis = redis;
    }

    // ✅ دریافت سبد خرید
    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return null;

            var items = cart.CartItems?.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? string.Empty,
                SellingPrice = ci.Product?.SellingPrice ?? 0,
                Quantity = ci.Quantity,
                TotalPrice = (ci.Product?.SellingPrice ?? 0) * ci.Quantity,
                Color = ci.Color,
                Size = ci.Size,
                ProductIcon = ci.Product?.Icon
            }).ToList() ?? new();

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CartItems = items,
                TotalItems = items.Sum(i => i.Quantity),
                TotalPrice = items.Sum(i => i.TotalPrice)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for user {UserId}", userId);
            return null;
        }
    }

    // ✅ ساخت سبد خرید جدید
    public async Task<CartDto?> CreateCartAsync(int userId)
    {
        try
        {
            var existing = await _context.TCarts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
            if (existing != null)
                return await GetCartByUserIdAsync(userId);

            var newCart = new TCarts { UserId = userId };
            _context.TCarts.Add(newCart);
            await _context.SaveChangesAsync();

            return new CartDto
            {
                Id = newCart.Id,
                UserId = newCart.UserId,
                CartItems = new(),
                TotalItems = 0,
                TotalPrice = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cart for user {UserId}", userId);
            return null;
        }
    }

    // ✅ افزودن محصول به سبد خرید (ساده، بدون تراکنش دستی)
    public async Task<CartOperationResult> AddItemToCartAsync(int userId, AddToCartDto dto)
    {
        try
        {
            if (dto == null || dto.Quantity <= 0 || dto.Quantity > 1000)
                return CartOperationResult.Error;

            var cart = await GetOrCreateCartAsync(userId);
            if (cart == null)
                return CartOperationResult.Error;

            var product = await _context.TProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null)
                return CartOperationResult.NotFound;

            if (!product.IsUnlimited && product.Count < dto.Quantity)
                return CartOperationResult.OutOfStock;

            var existingItem = await _context.TCartItems.FirstOrDefaultAsync(ci =>
                ci.CartId == cart.Id &&
                ci.ProductId == dto.ProductId &&
                (string.IsNullOrEmpty(dto.Color) || ci.Color == dto.Color) &&
                (string.IsNullOrEmpty(dto.Size) || ci.Size == dto.Size));

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (!product.IsUnlimited && product.Count < newQuantity)
                    return CartOperationResult.OutOfStock;

                existingItem.Quantity = newQuantity;
                _context.TCartItems.Update(existingItem);
            }
            else
            {
                await _context.TCartItems.AddAsync(new TCartItems
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    CartId = cart.Id,
                    Color = dto.Color?.Trim(),
                    Size = dto.Size?.Trim()
                });
            }

            await _context.SaveChangesAsync();
            return CartOperationResult.Success;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "DB update error while adding to cart (UserId={UserId}, ProductId={ProductId})", userId, dto.ProductId);
            return CartOperationResult.Error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding item to cart (UserId={UserId}, ProductId={ProductId})", userId, dto.ProductId);
            return CartOperationResult.Error;
        }
    }

    // ✅ به‌روزرسانی آیتم سبد خرید
    public async Task<bool> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto)
    {
        try
        {
            if (dto == null || dto.Quantity <= 0 || dto.Quantity > 1000)
                return false;

            var cartItem = await _context.TCartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);

            if (cartItem == null || cartItem.Product == null)
                return false;

            if (!cartItem.Product.IsUnlimited && cartItem.Product.Count < dto.Quantity)
                return false;

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item (UserId={UserId}, ItemId={ItemId})", userId, itemId);
            return false;
        }
    }

    // ✅ حذف آیتم از سبد
    public async Task<bool> RemoveItemFromCartAsync(int userId, int itemId)
    {
        try
        {
            var item = await _context.TCartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);

            if (item == null)
                return false;

            _context.TCartItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item (UserId={UserId}, ItemId={ItemId})", userId, itemId);
            return false;
        }
    }

    // ✅ پاک کردن کل سبد
    public async Task<bool> ClearCartAsync(int userId)
    {
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                return true;

            _context.TCartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return false;
        }
    }

    // ✅ شمارش آیتم‌ها در سبد
    public async Task<int> GetCartItemsCountAsync(int userId)
    {
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart count for user {UserId}", userId);
            return 0;
        }
    }

    // ✅ محدودیت درخواست‌ها با Redis
    public async Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis not connected. Skipping rate limiting for key {Key}", key);
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var window = TimeSpan.FromMinutes(windowMinutes);
            var windowStart = now - (long)window.TotalSeconds;

            var tran = db.CreateTransaction();
            _ = tran.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var countTask = tran.SortedSetLengthAsync(key);
            _ = tran.SortedSetAddAsync(key, now.ToString(), now);
            _ = tran.KeyExpireAsync(key, window);

            if (await tran.ExecuteAsync())
            {
                var count = await countTask;
                return count > maxAttempts;
            }

            return false;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error for key {Key}", key);
            return false;
        }
    }

    // ✅ گرفتن یا ساخت سبد
    private async Task<TCarts?> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.TCarts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null)
            return cart;

        var newCart = new TCarts { UserId = userId };
        await _context.TCarts.AddAsync(newCart);
        await _context.SaveChangesAsync();
        return newCart;
    }
}
