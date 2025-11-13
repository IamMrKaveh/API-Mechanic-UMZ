namespace MainApi.Services.Cart;

public enum CartOperationResult
{
    Success,
    NotFound,
    OutOfStock,
    Error,
    ConcurrencyConflict
}

public class CartService : ICartService
{
    private readonly MechanicContext _context;
    private readonly ILogger<CartService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly IMediaService _mediaService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(MechanicContext context, ILogger<CartService> logger, ICacheService cacheService, IAuditService auditService, IHttpContextAccessor httpContextAccessor, IMediaService mediaService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _mediaService = mediaService;
    }

    public async Task<CartDto?> GetCartAsync(int? userId, string? guestId = null)
    {
        var cacheKey = userId.HasValue ? $"cart:user:{userId}" : $"cart:guest:{guestId}";
        if (string.IsNullOrEmpty(guestId) && !userId.HasValue) return null;

        var cached = await _cacheService.GetAsync<CartDto>(cacheKey);
        if (cached != null) return cached;

        var cart = await GetCartQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));

        if (cart == null) return null;

        var dto = await MapCartToDtoAsync(cart);

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        return await GetCartAsync(userId, null);
    }

    public async Task<CartDto?> CreateCartAsync(int userId)
    {
        var newCart = new TCarts { UserId = userId, LastUpdated = DateTime.UtcNow };
        _context.TCarts.Add(newCart);
        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId, null);
        return await MapCartToDtoAsync(newCart);
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int userId, AddToCartDto dto)
    {
        return await AddItemToCartAsync(userId, null, dto);
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto)
    {
        return await UpdateCartItemAsync(userId, null, itemId, dto);
    }

    public async Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int userId, int itemId)
    {
        return await RemoveItemFromCartAsync(userId, null, itemId);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        return await ClearCartAsync(userId, null);
    }

    public async Task<int> GetCartItemsCountAsync(int userId)
    {
        return await GetCartItemsCountAsync(userId, null);
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int? userId, string? guestId, AddToCartDto dto)
    {
        if (dto.Quantity <= 0 || dto.Quantity > 1000)
            return (CartOperationResult.Error, null);

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var cart = await GetCartEntityAsync(userId, guestId);
            if (cart == null)
            {
                var newCartEntity = new TCarts { UserId = userId, GuestToken = guestId, LastUpdated = DateTime.UtcNow };
                _context.TCarts.Add(newCartEntity);
                await _context.SaveChangesAsync();
                cart = newCartEntity;
            }
            else
            {
                cart.LastUpdated = DateTime.UtcNow;
            }

            var variant = await _context.TProductVariant
                .FromSqlRaw("SELECT * FROM \"TProductVariant\" WHERE \"Id\" = {0} FOR UPDATE", dto.VariantId)
                .FirstOrDefaultAsync();

            if (variant == null || !variant.IsActive)
                return (CartOperationResult.NotFound, null);

            var existingItem = await _context.TCartItems
                .FirstOrDefaultAsync(ci =>
                    ci.CartId == cart.Id &&
                    ci.VariantId == dto.VariantId);

            int totalRequested = existingItem != null ? existingItem.Quantity + dto.Quantity : dto.Quantity;

            if (!variant.IsUnlimited && variant.Stock < totalRequested)
                return (CartOperationResult.OutOfStock, null);

            string action;
            string details;

            if (existingItem != null)
            {
                if (dto.RowVersion != null && dto.RowVersion.Length > 0)
                {
                    _context.Entry(existingItem).Property("RowVersion").OriginalValue = dto.RowVersion;
                }

                existingItem.Quantity = totalRequested;
                action = "UpdateCartItemQuantity";
                details = $"Updated quantity for variant {dto.VariantId} to {totalRequested}";
            }
            else
            {
                await _context.TCartItems.AddAsync(new TCartItems
                {
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    CartId = cart.Id,
                });
                action = "AddToCart";
                details = $"Added variant {dto.VariantId} with quantity {dto.Quantity}";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await InvalidateCartCache(userId, guestId);

            if (userId.HasValue)
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
                await _auditService.LogCartEventAsync(userId.Value, action, details, ip);
            }

            var updatedCart = await GetCartAsync(userId, guestId);
            return (CartOperationResult.Success, updatedCart);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Concurrency conflict during AddItemToCart for user {UserId} or guest {GuestId}, variant {VariantId}", userId, guestId, dto.VariantId);
            var freshCartDto = await GetCartAsync(userId, guestId);
            return (CartOperationResult.ConcurrencyConflict, freshCartDto);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int? userId, string? guestId, int itemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity < 0 || dto.Quantity > 1000)
            return (CartOperationResult.Error, null);

        var cartItem = await _context.TCartItems
            .Include(ci => ci.Variant)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ((userId.HasValue && ci.Cart!.UserId == userId) || (!string.IsNullOrEmpty(guestId) && ci.Cart!.GuestToken == guestId)));

        if (cartItem?.Variant == null)
            return (CartOperationResult.NotFound, null);

        if (cartItem.Cart != null)
        {
            cartItem.Cart.LastUpdated = DateTime.UtcNow;
        }

        if (dto.RowVersion != null && dto.RowVersion.Length > 0)
        {
            _context.Entry(cartItem).Property("RowVersion").OriginalValue = dto.RowVersion;
        }

        if (dto.Quantity == 0)
        {
            _context.TCartItems.Remove(cartItem);
        }
        else
        {
            if (!cartItem.Variant.IsUnlimited && cartItem.Variant.Stock < dto.Quantity)
                return (CartOperationResult.OutOfStock, null);
            cartItem.Quantity = dto.Quantity;
        }

        try
        {
            await _context.SaveChangesAsync();
            await InvalidateCartCache(userId, guestId);
            var updatedCart = await GetCartAsync(userId, guestId);

            if (userId.HasValue)
            {
                var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
                var action = dto.Quantity == 0 ? "RemoveCartItem" : "UpdateCartItemQuantity";
                var details = $"Item {itemId} quantity set to {dto.Quantity}";
                await _auditService.LogCartEventAsync(userId.Value, action, details, ip);
            }

            return (CartOperationResult.Success, updatedCart);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during UpdateCartItem for user {UserId} or guest {GuestId}, item {ItemId}", userId, guestId, itemId);
            var entry = ex.Entries.Single();
            await entry.ReloadAsync();
            var freshCart = await GetCartAsync(userId, guestId);
            return (CartOperationResult.ConcurrencyConflict, freshCart);
        }
    }

    public async Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int? userId, string? guestId, int itemId)
    {
        var item = await _context.TCartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ((userId.HasValue && ci.Cart!.UserId == userId) || (!string.IsNullOrEmpty(guestId) && ci.Cart!.GuestToken == guestId)));
        if (item == null)
            return (false, null);

        if (item.Cart != null)
        {
            item.Cart.LastUpdated = DateTime.UtcNow;
        }

        _context.TCartItems.Remove(item);
        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId, guestId);

        if (userId.HasValue)
        {
            var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
            await _auditService.LogCartEventAsync(userId.Value, "RemoveFromCart", $"Removed item {itemId}", ip);
        }

        var updatedCart = await GetCartAsync(userId, guestId);
        return (true, updatedCart);
    }

    public async Task<bool> ClearCartAsync(int? userId, string? guestId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));
        if (cart == null || !cart.CartItems.Any())
            return true;

        cart.LastUpdated = DateTime.UtcNow;
        _context.TCartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId, guestId);

        if (userId.HasValue)
        {
            var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
            await _auditService.LogCartEventAsync(userId.Value, "ClearCart", "Cart cleared", ip);
        }

        return true;
    }

    public async Task<int> GetCartItemsCountAsync(int? userId, string? guestId = null)
    {
        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return 0;

        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId.Value) || (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId));
        return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
    }

    public async Task<TCarts?> GetCartEntityAsync(int? userId, string? guestId)
    {
        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            return null;
        }

        return await _context.TCarts.FirstOrDefaultAsync(c =>
            (userId.HasValue && c.UserId == userId.Value) ||
            (!string.IsNullOrEmpty(guestId) && c.GuestToken == guestId)
        );
    }

    public async Task MergeCartAsync(int userId, string guestId)
    {
        var guestCart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.GuestToken == guestId);

        if (guestCart == null || !guestCart.CartItems.Any()) return;

        var userCart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (userCart == null)
        {
            guestCart.UserId = userId;
            guestCart.GuestToken = null;
            guestCart.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            userCart.LastUpdated = DateTime.UtcNow;
            foreach (var guestItem in guestCart.CartItems)
            {
                var variant = await _context.TProductVariant.FindAsync(guestItem.VariantId);
                if (variant == null || !variant.IsActive) continue;

                var userItem = userCart.CartItems.FirstOrDefault(ui => ui.VariantId == guestItem.VariantId);
                if (userItem != null)
                {
                    int totalQuantity = userItem.Quantity + guestItem.Quantity;
                    if (!variant.IsUnlimited && totalQuantity > variant.Stock)
                    {
                        totalQuantity = variant.Stock;
                    }
                    userItem.Quantity = totalQuantity;
                }
                else
                {
                    if (!variant.IsUnlimited && guestItem.Quantity > variant.Stock)
                    {
                        guestItem.Quantity = variant.Stock;
                    }
                    guestItem.CartId = userCart.Id;
                    _context.TCartItems.Update(guestItem);
                }
            }
            _context.TCarts.Remove(guestCart);
        }

        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId, guestId);
    }

    private IQueryable<TCarts> GetCartQuery()
    {
        return _context.TCarts
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

    private async Task<CartDto> MapCartToDtoAsync(TCarts cart)
    {
        var items = new List<CartItemDto>();

        foreach (var ci in cart.CartItems ?? Enumerable.Empty<TCartItems>())
        {
            var productIcon = await _mediaService.GetPrimaryImageUrlAsync("ProductVariant", ci.VariantId)
                              ?? await _mediaService.GetPrimaryImageUrlAsync("Product", ci.Variant.ProductId);

            var attributes = ci.Variant.VariantAttributes.ToDictionary(
                va => va.AttributeValue.AttributeType.Name.ToLower(),
                va => new AttributeValueDto(
                    va.AttributeValueId,
                    va.AttributeValue.AttributeType.Name,
                    va.AttributeValue.AttributeType.DisplayName,
                    va.AttributeValue.Value,
                    va.AttributeValue.DisplayValue,
                    va.AttributeValue.HexCode
                )
            );

            var item = new CartItemDto(
                Id: ci.Id,
                VariantId: ci.VariantId,
                ProductName: ci.Variant.Product.Name,
                SellingPrice: ci.Variant.SellingPrice,
                Quantity: ci.Quantity,
                ProductIcon: productIcon,
                TotalPrice: ci.Variant.SellingPrice * ci.Quantity,
                RowVersion: ci.RowVersion,
                Attributes: attributes
            );

            items.Add(item);
        }

        var cartDto = new CartDto(
            Id: cart.Id,
            UserId: cart.UserId,
            GuestToken: cart.GuestToken,
            CartItems: items,
            TotalItems: items.Sum(i => i.Quantity),
            TotalPrice: items.Sum(i => i.TotalPrice)
        );

        return cartDto;
    }

    private async Task InvalidateCartCache(int? userId, string? guestId)
    {
        if (userId.HasValue)
        {
            var cacheKey = $"cart:user:{userId}";
            await _cacheService.ClearAsync(cacheKey);
        }
        if (!string.IsNullOrEmpty(guestId))
        {
            var cacheKey = $"cart:guest:{guestId}";
            await _cacheService.ClearAsync(cacheKey);
        }
    }
}