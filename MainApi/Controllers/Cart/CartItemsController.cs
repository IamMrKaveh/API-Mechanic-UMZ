namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartItemsController : ControllerBase
{
    private readonly MechanicContext _context;
    private readonly ILogger<CartItemsController> _logger;

    public CartItemsController(MechanicContext context, ILogger<CartItemsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCartItems()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        try
        {
            var cartItems = await _context.TCartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Cart)
                .Where(ci => ci.Cart!.UserId == userId)
                .Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product!.Name ?? "",
                    SellingPrice = ci.Product.SellingPrice ?? 0,
                    Quantity = ci.Quantity,
                    TotalPrice = (ci.Product.SellingPrice ?? 0) * ci.Quantity
                })
                .ToListAsync();

            return Ok(cartItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart items for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving cart items");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CartItemDto>> GetCartItem(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        try
        {
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Cart)
                .Where(ci => ci.Id == id && ci.Cart!.UserId == userId)
                .Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product!.Name ?? "",
                    SellingPrice = ci.Product.SellingPrice ?? 0,
                    Quantity = ci.Quantity,
                    TotalPrice = (ci.Product.SellingPrice ?? 0) * ci.Quantity
                })
                .FirstOrDefaultAsync();

            if (cartItem == null)
                return NotFound("Cart item not found");

            return Ok(cartItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart item {ItemId} for user {UserId}", id, userId);
            return StatusCode(500, "An error occurred while retrieving cart item");
        }
    }

    [HttpPost]
    public async Task<ActionResult<CartItemDto>> CreateCartItem([FromBody] AddToCartDto addToCartDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        if (addToCartDto.Quantity > 1000)
            return BadRequest("Quantity cannot exceed 1000");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.TProducts
                .FirstOrDefaultAsync(p => p.Id == addToCartDto.ProductId);

            if (product == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Product not found");
            }

            var currentStock = product.Count ?? 0;
            if (currentStock < addToCartDto.Quantity)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Not enough stock available. Current stock: {currentStock}");
            }

            var cart = await _context.TCarts
                .Include(c => c.CartItems)
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

            var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == addToCartDto.ProductId);

            if (existingCartItem != null)
            {
                var newQuantity = existingCartItem.Quantity + addToCartDto.Quantity;
                if (currentStock < newQuantity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Not enough stock available for updated quantity. Current stock: {currentStock}");
                }
                existingCartItem.Quantity = newQuantity;
                _context.TCartItems.Update(existingCartItem);
            }
            else
            {
                existingCartItem = new TCartItems
                {
                    CartId = cart.Id,
                    ProductId = addToCartDto.ProductId,
                    Quantity = addToCartDto.Quantity
                };
                _context.TCartItems.Add(existingCartItem);
            }

            var reloadedProduct = await _context.TProducts
                .FirstOrDefaultAsync(p => p.Id == addToCartDto.ProductId);

            if (reloadedProduct == null || (reloadedProduct.Count ?? 0) < (existingCartItem.Quantity))
            {
                await transaction.RollbackAsync();
                return BadRequest($"Stock changed during operation. Please try again.");
            }

            await UpdateCartTotalsAsync(cart.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var cartItemDto = new CartItemDto
            {
                Id = existingCartItem.Id,
                ProductId = existingCartItem.ProductId,
                ProductName = product.Name ?? "",
                SellingPrice = product.SellingPrice ?? 0,
                Quantity = existingCartItem.Quantity,
                TotalPrice = (product.SellingPrice ?? 0) * existingCartItem.Quantity
            };

            _logger.LogInformation("Added item to cart: ProductId {ProductId}, Quantity {Quantity}, UserId {UserId}",
                addToCartDto.ProductId, addToCartDto.Quantity, userId);

            return CreatedAtAction(nameof(GetCartItem), new { id = existingCartItem.Id }, cartItemDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error adding item to cart: ProductId {ProductId}, UserId {UserId}", addToCartDto.ProductId, userId);
            return StatusCode(500, "An error occurred while adding item to cart");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemDto updateCartItemDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        if (updateCartItemDto.Quantity > 1000)
            return BadRequest("Quantity cannot exceed 1000");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart!.UserId == userId);

            if (cartItem == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Cart item not found");
            }

            var product = await _context.TProducts
                .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId);

            if (product == null || (product.Count ?? 0) < updateCartItemDto.Quantity)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Not enough stock available. Current stock: {product?.Count ?? 0}");
            }

            cartItem.Quantity = updateCartItemDto.Quantity;
            _context.TCartItems.Update(cartItem);

            await UpdateCartTotalsAsync(cartItem.CartId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Updated cart item: ItemId {ItemId}, Quantity {Quantity}, UserId {UserId}",
                id, updateCartItemDto.Quantity, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating cart item: ItemId {ItemId}, UserId {UserId}", id, userId);
            return StatusCode(500, "An error occurred while updating cart item");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCartItem(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cartItem = await _context.TCartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.Cart!.UserId == userId);

            if (cartItem == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Cart item not found");
            }

            var cartId = cartItem.CartId;
            _context.TCartItems.Remove(cartItem);

            await UpdateCartTotalsAsync(cartId);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deleted cart item: ItemId {ItemId}, UserId {UserId}", id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting cart item: ItemId {ItemId}, UserId {UserId}", id, userId);
            return StatusCode(500, "An error occurred while deleting cart item");
        }
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await _context.TCarts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Cart not found");
            }

            _context.TCartItems.RemoveRange(cart.CartItems);
            cart.TotalItems = 0;
            cart.TotalPrice = 0;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Cleared cart for user {UserId}", userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return StatusCode(500, "An error occurred while clearing cart");
        }
    }

    [NonAction]
    private async Task UpdateCartTotalsAsync(int cartId)
    {
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart != null)
        {
            cart.TotalItems = cart.CartItems.Sum(ci => ci.Quantity);
            cart.TotalPrice = cart.CartItems.Sum(ci => (ci.Product!.SellingPrice ?? 0) * ci.Quantity);
            _context.TCarts.Update(cart);
        }
    }

    [NonAction]
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return 0;
        return userId;
    }
}