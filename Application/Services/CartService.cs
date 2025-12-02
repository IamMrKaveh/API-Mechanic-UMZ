namespace Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<CartService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IAuditService _auditService;
    private readonly IMediaService _mediaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CartService(
        ICartRepository cartRepository,
        ILogger<CartService> logger,
        ICacheService cacheService,
        IAuditService auditService,
        IMediaService mediaService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _logger = logger;
        _cacheService = cacheService;
        _auditService = auditService;
        _mediaService = mediaService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CartDto?> GetCartAsync(int? userId, string? guestId = null)
    {
        var cacheKey = userId.HasValue ? $"cart:user:{userId}" : $"cart:guest:{guestId}";
        if (string.IsNullOrEmpty(guestId) && !userId.HasValue) return null;

        var cached = await _cacheService.GetAsync<CartDto>(cacheKey);
        if (cached != null) return cached;

        var cart = await _cartRepository.GetCartAsync(userId, guestId);

        if (cart == null) return null;

        var dto = await MapCartToDtoAsync(cart);

        var productTags = cart.CartItems
            .Select(ci => $"product:{ci.VariantId}")
            .Distinct()
            .ToList();

        if (userId.HasValue)
        {
            productTags.Add($"cart:user:{userId.Value}");
        }

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), productTags);
        return dto;
    }

    public async Task<CartDto?> GetCartByUserIdAsync(int userId)
    {
        return await GetCartAsync(userId, null);
    }

    public async Task<CartDto?> CreateCartAsync(int userId)
    {
        var newCart = new Cart { UserId = userId, LastUpdated = DateTime.UtcNow };
        await _cartRepository.AddCartAsync(newCart);
        await _unitOfWork.SaveChangesAsync();
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

        try
        {
            var cart = await GetCartEntityAsync(userId, guestId);
            if (cart == null)
            {
                var newCartEntity = new Cart { UserId = userId, GuestToken = guestId, LastUpdated = DateTime.UtcNow };
                await _cartRepository.AddCartAsync(newCartEntity);
                await _unitOfWork.SaveChangesAsync();
                cart = newCartEntity;
            }
            else
            {
                cart.LastUpdated = DateTime.UtcNow;
            }

            var variant = await _cartRepository.GetVariantByIdAsync(dto.VariantId);

            if (variant == null || !variant.IsActive)
                return (CartOperationResult.NotFound, null);

            var existingItem = await _cartRepository.GetCartItemAsync(cart.Id, dto.VariantId);

            int totalRequested = existingItem != null ? existingItem.Quantity + dto.Quantity : dto.Quantity;

            if (!variant.IsUnlimited && variant.Stock < totalRequested)
                return (CartOperationResult.OutOfStock, null);

            string action;
            string details;

            if (existingItem != null)
            {
                if (dto.CartItemRowVersion != null && dto.CartItemRowVersion.Length > 0)
                {
                    _cartRepository.SetCartItemRowVersion(existingItem, dto.CartItemRowVersion);
                }

                existingItem.Quantity = totalRequested;
                action = "UpdateCartItemQuantity";
                details = $"Updated quantity for variant {dto.VariantId} to {totalRequested}";
            }
            else
            {
                await _cartRepository.AddCartItemAsync(new CartItem
                {
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    CartId = cart.Id,
                });
                action = "AddToCart";
                details = $"Added variant {dto.VariantId} with quantity {dto.Quantity}";
            }

            await _unitOfWork.SaveChangesAsync();

            await InvalidateCartCache(userId, guestId);

            if (userId.HasValue)
            {
                var ip = _currentUserService.IpAddress ?? "N/A";
                await _auditService.LogCartEventAsync(userId.Value, action, details, ip);
            }

            var updatedCart = await GetCartAsync(userId, guestId);
            return (CartOperationResult.Success, updatedCart);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during AddItemToCart for user {UserId} or guest {GuestId}, variant {VariantId}", userId, guestId, dto.VariantId);
            var freshCartDto = await GetCartAsync(userId, guestId);
            return (CartOperationResult.ConcurrencyConflict, freshCartDto);
        }
    }

    public async Task<(CartOperationResult Result, CartDto? Cart)> UpdateCartItemAsync(int? userId, string? guestId, int itemId, UpdateCartItemDto dto)
    {
        if (dto.Quantity < 0 || dto.Quantity > 1000)
            return (CartOperationResult.Error, null);

        var cartItem = await _cartRepository.GetCartItemWithDetailsAsync(itemId, userId, guestId);

        if (cartItem?.Variant == null)
            return (CartOperationResult.NotFound, null);

        if (cartItem.Cart != null)
        {
            cartItem.Cart.LastUpdated = DateTime.UtcNow;
        }

        if (dto.RowVersion != null && dto.RowVersion.Length > 0)
        {
            _cartRepository.SetCartItemRowVersion(cartItem, dto.RowVersion);
        }

        if (dto.Quantity == 0)
        {
            _cartRepository.RemoveCartItem(cartItem);
        }
        else
        {
            if (!cartItem.Variant.IsUnlimited && cartItem.Variant.Stock < dto.Quantity)
                return (CartOperationResult.OutOfStock, null);
            cartItem.Quantity = dto.Quantity;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            await InvalidateCartCache(userId, guestId);
            var updatedCart = await GetCartAsync(userId, guestId);

            if (userId.HasValue)
            {
                var ip = _currentUserService.IpAddress ?? "N/A";
                var action = dto.Quantity == 0 ? "RemoveCartItem" : "UpdateCartItemQuantity";
                var details = $"Item {itemId} quantity set to {dto.Quantity}";
                await _auditService.LogCartEventAsync(userId.Value, action, details, ip);
            }

            return (CartOperationResult.Success, updatedCart);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during UpdateCartItem for user {UserId} or guest {GuestId}, item {ItemId}", userId, guestId, itemId);
            await InvalidateCartCache(userId, guestId);
            var freshCart = await GetCartAsync(userId, guestId);
            return (CartOperationResult.ConcurrencyConflict, freshCart);
        }
    }

    public async Task<(bool Success, CartDto? Cart)> RemoveItemFromCartAsync(int? userId, string? guestId, int itemId)
    {
        var item = await _cartRepository.GetCartItemWithDetailsAsync(itemId, userId, guestId);
        if (item == null)
            return (false, null);

        if (item.Cart != null)
        {
            item.Cart.LastUpdated = DateTime.UtcNow;
        }

        _cartRepository.RemoveCartItem(item);
        await _unitOfWork.SaveChangesAsync();
        await InvalidateCartCache(userId, guestId);

        if (userId.HasValue)
        {
            var ip = _currentUserService.IpAddress ?? "N/A";
            await _auditService.LogCartEventAsync(userId.Value, "RemoveFromCart", $"Removed item {itemId}", ip);
        }

        var updatedCart = await GetCartAsync(userId, guestId);
        return (true, updatedCart);
    }

    public async Task<bool> ClearCartAsync(int? userId, string? guestId)
    {
        var cart = await _cartRepository.GetCartAsync(userId, guestId);
        if (cart == null || !cart.CartItems.Any())
            return true;

        cart.LastUpdated = DateTime.UtcNow;
        _cartRepository.RemoveCartItems(cart.CartItems);
        await _unitOfWork.SaveChangesAsync();
        await InvalidateCartCache(userId, guestId);

        if (userId.HasValue)
        {
            var ip = _currentUserService.IpAddress ?? "N/A";
            await _auditService.LogCartEventAsync(userId.Value, "ClearCart", "Cart cleared", ip);
        }

        return true;
    }

    public async Task<int> GetCartItemsCountAsync(int? userId, string? guestId = null)
    {
        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return 0;

        return await _cartRepository.GetCartItemsCountAsync(userId, guestId);
    }

    public async Task<Cart?> GetCartEntityAsync(int? userId, string? guestId)
    {
        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            return null;
        }

        return await _cartRepository.GetCartEntityAsync(userId, guestId);
    }

    public async Task MergeCartAsync(int userId, string guestId)
    {
        var guestCart = await _cartRepository.GetCartAsync(null, guestId);
        if (guestCart == null || !guestCart.CartItems.Any()) return;

        var userCart = await _cartRepository.GetCartAsync(userId, null);
        Domain.Cart.Cart userCartEntity;

        if (userCart == null)
        {
            userCartEntity = new Domain.Cart.Cart
            {
                UserId = userId,
                LastUpdated = DateTime.UtcNow,
                GuestToken = null
            };
            await _cartRepository.AddCartAsync(userCartEntity);
            await _unitOfWork.SaveChangesAsync();
        }
        else
        {
            userCartEntity = await _cartRepository.GetCartEntityAsync(userId, null) ?? new Domain.Cart.Cart { UserId = userId };
            userCartEntity.LastUpdated = DateTime.UtcNow;
        }

        foreach (var guestItem in guestCart.CartItems)
        {
            var variant = await _cartRepository.GetVariantByIdAsync(guestItem.VariantId);
            if (variant == null || !variant.IsActive) continue;

            var existingUserItem = await _cartRepository.GetCartItemAsync(userCartEntity.Id, guestItem.VariantId);

            if (existingUserItem != null)
            {
                int newQuantity = existingUserItem.Quantity + guestItem.Quantity;
                if (!variant.IsUnlimited && newQuantity > variant.Stock)
                {
                    newQuantity = variant.Stock;
                }

                existingUserItem.Quantity = newQuantity;
            }
            else
            {
                int quantity = guestItem.Quantity;
                if (!variant.IsUnlimited && quantity > variant.Stock)
                {
                    quantity = variant.Stock;
                }

                var newItem = new Domain.Cart.CartItem
                {
                    CartId = userCartEntity.Id,
                    VariantId = guestItem.VariantId,
                    Quantity = quantity,
                    RowVersion = guestItem.RowVersion
                };

                await _cartRepository.AddCartItemAsync(newItem);
            }
        }

        var guestCartEntity = await _cartRepository.GetCartEntityAsync(null, guestId);
        if (guestCartEntity != null)
        {
            _cartRepository.RemoveCart(guestCartEntity);
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.ClearAsync($"cart:user:{userId}");
        await _cacheService.ClearAsync($"cart:guest:{guestId}");
    }

    private async Task<CartDto> MapCartToDtoAsync(Cart cart)
    {
        var items = new List<CartItemDto>();
        var itemsToRemove = new List<CartItem>();

        foreach (var ci in cart.CartItems ?? Enumerable.Empty<CartItem>())
        {
            if (ci.Variant == null || ci.Variant.Product == null)
            {
                itemsToRemove.Add(ci);
                continue;
            }

            string? productIcon = ci.Variant.Images.FirstOrDefault(i => i.IsPrimary)?.FilePath
                                      ?? ci.Variant.Images.FirstOrDefault()?.FilePath;

            if (string.IsNullOrEmpty(productIcon))
            {
                productIcon = await _mediaService.GetPrimaryImageUrlAsync("Product", ci.Variant.ProductId);
            }
            else
            {
                productIcon = _mediaService.GetUrl(productIcon);
            }

            var attributes = ci.Variant.VariantAttributes.ToDictionary(
                va => va.AttributeValue.AttributeType.Name.ToLower(),
                va => new AttributeValueDto(
                    va.AttributeValueId,
                    va.AttributeValue.AttributeType.Name,
                    va.AttributeValue.AttributeType.DisplayName,
                    va.AttributeValue.Value,
                    va.AttributeValue.DisplayValue,
                    va.AttributeValue.HexCode ?? string.Empty
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

        if (itemsToRemove.Any())
        {
            _cartRepository.RemoveCartItems(itemsToRemove);
            await _unitOfWork.SaveChangesAsync();
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