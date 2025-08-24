namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartItemsController : ControllerBase
{
    private readonly MechanicContext _context;

    public CartItemsController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet("cart-items")]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCartItems()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        var cartItems = await _context.TCartItems
            .Include(ci => ci.Product)
            .Include(ci => ci.Cart)
            .Where(ci => ci.Cart.UserId == userId)
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name ?? "",
                SellingPrice = ci.Product.SellingPrice ?? 0,
                Quantity = ci.Quantity,
                TotalPrice = (ci.Product.SellingPrice ?? 0) * ci.Quantity
            })
            .ToListAsync();

        return Ok(cartItems);
    }

    [HttpGet("cart-items/{id}")]
    public async Task<ActionResult<CartItemDto>> GetCartItem(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        var cartItem = await _context.TCartItems
            .Include(ci => ci.Product)
            .Include(ci => ci.Cart)
            .Where(ci => ci.Id == id && ci.Cart.UserId == userId)
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name ?? "",
                SellingPrice = ci.Product.SellingPrice ?? 0,
                Quantity = ci.Quantity,
                TotalPrice = (ci.Product.SellingPrice ?? 0) * ci.Quantity
            })
            .FirstOrDefaultAsync();

        if (cartItem == null)
            return NotFound("Cart item not found");

        return Ok(cartItem);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return 0;
        return userId;
    }
}