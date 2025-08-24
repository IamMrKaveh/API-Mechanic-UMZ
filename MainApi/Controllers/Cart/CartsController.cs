namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly MechanicContext _context;
    public CartsController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet("my-cart")]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null)
        {
            cart = new TCarts { UserId = userId, TotalItems = 0, TotalPrice = 0 };
            _context.TCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var cartDto = new CartDto
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

        return Ok(cartDto);
    }

    [HttpPost("add-item")]
    public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new TCarts { UserId = userId };
            _context.TCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        try
        {
            var product = await _context.TProducts.FindAsync(dto.ProductId);
            if (product == null)
                return NotFound("Product not found");
            if ((product.Count ?? 0) < dto.Quantity)
                return BadRequest("Insufficient stock");

            var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;
                if (newQuantity > (product.Count ?? 0))
                {
                    return BadRequest("Total quantity exceeds available stock");
                }
                existingItem.Quantity = newQuantity;
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

            cart.TotalItems += dto.Quantity;
            cart.TotalPrice += (product.SellingPrice ?? 0) * dto.Quantity;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Item added to cart successfully" });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The stock for this item has just changed. Please try again.");
        }
    }

    [HttpPut("update-item/{itemId}")]
    public async Task<ActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var cartItem = await _context.TCartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);
        if (cartItem == null)
            return NotFound("Cart item not found");
        if (dto.Quantity > (cartItem.Product?.Count ?? 0))
            return BadRequest("Quantity exceeds available stock");

        int quantityDifference = dto.Quantity - cartItem.Quantity;
        cartItem.Cart.TotalItems += quantityDifference;
        cartItem.Cart.TotalPrice += (cartItem.Product?.SellingPrice ?? 0) * quantityDifference;
        cartItem.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cart item updated successfully" });
    }

    [HttpDelete("remove-item/{itemId}")]
    public async Task<ActionResult> RemoveFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var cartItem = await _context.TCartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);
        if (cartItem == null)
            return NotFound("Cart item not found");

        cartItem.Cart.TotalItems -= cartItem.Quantity;
        cartItem.Cart.TotalPrice -= (cartItem.Product?.SellingPrice ?? 0) * cartItem.Quantity;

        _context.TCartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item removed from cart successfully" });
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null || !cart.CartItems.Any())
            return Ok(new { Message = "Cart is already empty" });

        _context.TCartItems.RemoveRange(cart.CartItems);
        cart.TotalItems = 0;
        cart.TotalPrice = 0;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Cart cleared successfully" });
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var count = await _context.TCarts
            .Where(c => c.UserId == userId)
            .Select(c => c.TotalItems)
            .FirstOrDefaultAsync();
        return Ok(count);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return 0;
        return userId;
    }
}