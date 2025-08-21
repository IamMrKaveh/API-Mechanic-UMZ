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

        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new TCarts { UserId = userId };
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
            TotalItems = cart.CartItems?.Sum(ci => ci.Quantity) ?? 0,
            TotalPrice = cart.CartItems?.Sum(ci => (ci.Product?.SellingPrice ?? 0) * ci.Quantity) ?? 0
        };

        return Ok(cartDto);
    }

    [HttpPost("add-item")]
    public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        var product = await _context.TProducts.FindAsync(dto.ProductId);
        if (product == null)
            return NotFound("Product not found");

        if ((product.Count ?? 0) < dto.Quantity)
            return BadRequest("Insufficient stock");

        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new TCarts { UserId = userId };
            _context.TCarts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
            if (existingItem.Quantity > product.Count)
            {
                return BadRequest("Total quantity exceeds available stock");
            }
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

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Item added to cart successfully" });
    }

    [HttpPut("update-item/{itemId}")]
    public async Task<ActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        var cartItem = await _context.TCartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

        if (cartItem == null)
            return NotFound("Cart item not found");

        if (dto.Quantity <= 0)
        {
            _context.TCartItems.Remove(cartItem);
        }
        else
        {
            if (dto.Quantity > (cartItem.Product.Count ?? 0))
                return BadRequest("Quantity exceeds available stock");

            cartItem.Quantity = dto.Quantity;
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cart item updated successfully" });
    }

    [HttpDelete("remove-item/{itemId}")]
    public async Task<ActionResult> RemoveFromCart(int itemId)
    {
        var userId = GetCurrentUserId();

        var cartItem = await _context.TCartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.UserId == userId);

        if (cartItem == null)
            return NotFound("Cart item not found");

        _context.TCartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item removed from cart successfully" });
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();

        var cart = await _context.TCarts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
            return Ok(new { Message = "Cart is already empty" });

        _context.TCartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Cart cleared successfully" });
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = GetCurrentUserId();

        var count = await _context.TCartItems
            .Where(ci => ci.Cart.UserId == userId)
            .SumAsync(ci => ci.Quantity);

        return Ok(count);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
