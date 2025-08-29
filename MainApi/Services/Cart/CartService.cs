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
                cart = new TCarts
                {
                    UserId = userId,
                    TotalItems = 0,
                    TotalPrice = 0
                };
                _context.TCarts.Add(cart);
                await _context.SaveChangesAsync();
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
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto.Quantity <= 0 || dto.Quantity > 1000)
            {
                _logger.LogWarning("Invalid quantity in cart request: {Quantity}", dto.Quantity);
                return false;
            }

            var cart = await GetOrCreateCartAsync(userId);

            var product = await _context.TProducts
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
            {
                _logger.LogWarning("Product not found when adding to cart: ProductId {ProductId}, UserId {UserId}", dto.ProductId, userId);
                return false;
            }

            if ((product.SellingPrice ?? 0) <= 0)
            {
                _logger.LogWarning("Product has invalid selling price: ProductId {ProductId}", dto.ProductId);
                return false;
            }

            var currentStock = product.Count ?? 0;
            if (currentStock < dto.Quantity)
            {
                _logger.LogWarning("Insufficient stock when adding to cart: ProductId {ProductId}, Available {Available}, Requested {Requested}",
                    dto.ProductId, currentStock, dto.Quantity);
                return false;
            }

            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (newQuantity > currentStock || newQuantity > 1000)
                {
                    _logger.LogWarning("Invalid total quantity for cart item: ProductId {ProductId}, NewQuantity {NewQuantity}",
                        dto.ProductId, newQuantity);
                    return false;
                }

                existingItem.Quantity = newQuantity;
                _context.TCartItems.Update(existingItem);
            }
            else
            {
                var cartItem = new TCartItems
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _context.TCartItems.Add(cartItem);
            }

            await UpdateCartTotalsAsync(cart);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Added item to cart: ProductId {ProductId}, Quantity {Quantity}, UserId {UserId}",
                dto.ProductId, dto.Quantity, userId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding item to cart: ProductId {ProductId}, UserId {UserId}", dto.ProductId, userId);
            return false;
        }
    }

    private async Task UpdateCartTotalsAsync(TCarts cart)
    {
        var cartItems = await _context.TCartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync();

        cart.TotalItems = cartItems.Sum(ci => ci.Quantity);
        cart.TotalPrice = cartItems.Sum(ci => (ci.Product?.SellingPrice ?? 0) * ci.Quantity);

        _context.TCarts.Update(cart);
    }

    private async Task<TCarts> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new TCarts { UserId = userId, TotalItems = 0, TotalPrice = 0 };
            _context.TCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<bool> UpdateCartItemAsync(int userId, int itemId, UpdateCartItemDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto.Quantity <= 0 || dto.Quantity > 1000)
            {
                _logger.LogWarning("Invalid quantity for cart item update: {Quantity}", dto.Quantity);
                return false;
            }
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);
            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found: ItemId {ItemId}, UserId {UserId}", itemId, userId);
                return false;
            }
            var product = await _context.TProducts
                .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);
            if (product == null || dto.Quantity > (product.Count ?? 0))
            {
                _logger.LogWarning("Insufficient stock for cart item update: ItemId {ItemId}, Available {Available}, Requested {Requested}",
                    itemId, product?.Count ?? 0, dto.Quantity);
                return false;
            }
            cartItem.Quantity = dto.Quantity;
            _context.TCartItems.Update(cartItem);
            await UpdateCartTotalsAsync(cartItem.Cart!);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Updated cart item: ItemId {ItemId}, Quantity {Quantity}, UserId {UserId}",
                itemId, dto.Quantity, userId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating cart item: ItemId {ItemId}, UserId {UserId}", itemId, userId);
            return false;
        }
    }

    public async Task<bool> RemoveItemFromCartAsync(int userId, int itemId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart!.UserId == userId);
            if (cartItem == null)
            {
                _logger.LogWarning("Cart item not found for removal: ItemId {ItemId}, UserId {UserId}", itemId, userId);
                return false;
            }
            _context.TCartItems.Remove(cartItem);
            await UpdateCartTotalsAsync(cartItem.Cart!);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogInformation("Removed cart item: ItemId {ItemId}, UserId {UserId}", itemId, userId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error removing cart item: ItemId {ItemId}, UserId {UserId}", itemId, userId);
            return false;
        }
    }

    public async Task<bool> ClearCartAsync(int userId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                _logger.LogInformation("Cart already empty or not found: UserId {UserId}", userId);
                return true;
            }

            _context.TCartItems.RemoveRange(cart.CartItems);
            cart.TotalItems = 0;
            cart.TotalPrice = 0;
            _context.TCarts.Update(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Cleared cart: UserId {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error clearing cart: UserId {UserId}", userId);
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
            _logger.LogError(ex, "Error getting cart items count: UserId {UserId}", userId);
            return 0;
        }
    }
}