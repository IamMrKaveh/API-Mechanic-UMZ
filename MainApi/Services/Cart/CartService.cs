using MainApi.Services.Media;

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

    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        var cacheKey = $"cart:user:{userId}";
        var cached = await _cacheService.GetAsync<CartDto>(cacheKey);
        if (cached != null) return cached;

        var cart = await _context.TCarts
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
                            .ThenInclude(av => av.AttributeType)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return null;

        var items = new List<CartItemDto>();
        foreach (var ci in cart.CartItems ?? Enumerable.Empty<TCartItems>())
        {
            items.Add(new CartItemDto
            {
                Id = ci.Id,
                VariantId = ci.VariantId,
                ProductName = ci.Variant.Product.Name,
                SellingPrice = ci.Variant.SellingPrice,
                Quantity = ci.Quantity,
                TotalPrice = ci.Variant.SellingPrice * ci.Quantity,
                ProductIcon = await _mediaService.GetPrimaryImageUrlAsync("ProductVariant", ci.VariantId) ?? await _mediaService.GetPrimaryImageUrlAsync("Product", ci.Variant.ProductId),
                RowVersion = ci.RowVersion,
                Attributes = ci.Variant.VariantAttributes.ToDictionary(
                    va => va.AttributeValue.AttributeType.Name.ToLower(),
                    va => new AttributeValueDto
                    {
                        Id = va.AttributeValueId,
                        Type = va.AttributeValue.AttributeType.Name,
                        TypeDisplay = va.AttributeValue.AttributeType.DisplayName,
                        Value = va.AttributeValue.Value,
                        DisplayValue = va.AttributeValue.DisplayValue,
                        HexCode = va.AttributeValue.HexCode
                    })
            });
        }


        var dto = new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            CartItems = items,
            TotalItems = items.Sum(i => i.Quantity),
            TotalPrice = items.Sum(i => i.TotalPrice)
        };

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task<CartDto?> CreateCartAsync(int userId)
    {
        var existing = await _context.TCarts.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
        if (existing != null)
            return await GetCartByUserIdAsync(userId);

        var newCart = new TCarts { UserId = userId };
        _context.TCarts.Add(newCart);
        await _context.SaveChangesAsync();

        await InvalidateCartCache(userId);
        return new CartDto
        {
            Id = newCart.Id,
            UserId = newCart.UserId,
            CartItems = new(),
            TotalItems = 0,
            TotalPrice = 0
        };
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> AddItemToCartAsync(int userId, AddToCartDto dto)
    {
        if (dto.Quantity <= 0 || dto.Quantity > 1000)
            return (CartOperationResult.Error, null);

        var cart = await GetOrCreateCartAsync(userId);
        if (cart == null)
            return (CartOperationResult.Error, null);

        var variant = await _context.TProductVariant
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.IsActive);

        if (variant == null)
            return (CartOperationResult.NotFound, null);

        var existingItem = await _context.TCartItems
            .FirstOrDefaultAsync(ci =>
                ci.CartId == cart.Id &&
                ci.VariantId == dto.VariantId);

        string action;
        string details;

        try
        {
            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (!variant.IsUnlimited && variant.Stock < newQuantity)
                    return (CartOperationResult.OutOfStock, null);

                if (dto.RowVersion != null && dto.RowVersion.Length > 0)
                {
                    _context.Entry(existingItem).Property("RowVersion").OriginalValue = dto.RowVersion;
                }

                existingItem.Quantity = newQuantity;
                action = "UpdateCartItemQuantity";
                details = $"Updated quantity for variant {dto.VariantId} to {newQuantity}";
            }
            else
            {
                if (!variant.IsUnlimited && variant.Stock < dto.Quantity)
                    return (CartOperationResult.OutOfStock, null);

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
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during AddItemToCart for user {UserId}, variant {VariantId}", userId, dto.VariantId);
            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync();
            }
            var freshCartDto = await GetCartByUserIdAsync(userId);
            return (CartOperationResult.ConcurrencyConflict, freshCartDto);
        }

        await InvalidateCartCache(userId);

        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "N/A";
        await _auditService.LogCartEventAsync(userId, action, details, ip);

        var updatedCart = await GetCartByUserIdAsync(userId);
        return (CartOperationResult.Success, updatedCart);
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity < 0 || dto.Quantity > 1000)
            return (CartOperationResult.Error, null);

        var cartItem = await _context.TCartItems
            .Include(ci => ci.Variant)
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);

        if (cartItem?.Variant == null)
            return (CartOperationResult.NotFound, null);

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
            await InvalidateCartCache(userId);
            var updatedCart = await GetCartByUserIdAsync(userId);

            var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
            var action = dto.Quantity == 0 ? "RemoveCartItem" : "UpdateCartItemQuantity";
            var details = $"Item {itemId} quantity set to {dto.Quantity}";
            await _auditService.LogCartEventAsync(userId, action, details, ip);

            return (CartOperationResult.Success, updatedCart);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during UpdateCartItem for user {UserId}, item {ItemId}", userId, itemId);
            var entry = ex.Entries.Single();
            await entry.ReloadAsync();
            var freshCart = await GetCartByUserIdAsync(userId);
            return (CartOperationResult.ConcurrencyConflict, freshCart);
        }
    }

    public async Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int userId, int itemId)
    {
        var item = await _context.TCartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);
        if (item == null)
            return (false, null);

        _context.TCartItems.Remove(item);
        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId);

        var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
        await _auditService.LogCartEventAsync(userId, "RemoveFromCart", $"Removed item {itemId}", ip);

        var updatedCart = await GetCartByUserIdAsync(userId);
        return (true, updatedCart);
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null || !cart.CartItems.Any())
            return true;

        _context.TCartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();
        await InvalidateCartCache(userId);

        var ip = HttpContextHelper.GetClientIpAddress(_httpContextAccessor.HttpContext);
        await _auditService.LogCartEventAsync(userId, "ClearCart", "Cart cleared", ip);

        return true;
    }

    public async Task<int> GetCartItemsCountAsync(int userId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);
        return cart?.CartItems?.Sum(ci => ci.Quantity) ?? 0;
    }

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

    private async Task InvalidateCartCache(int userId)
    {
        var cacheKey = $"cart:user:{userId}";
        await _cacheService.ClearAsync(cacheKey);
    }
}