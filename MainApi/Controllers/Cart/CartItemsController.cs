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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartItemDto>>> GetCartItems()
    {
        var userId = GetCurrentUserId();

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

    [HttpGet("{id}")]
    public async Task<ActionResult<CartItemDto>> GetCartItem(int id)
    {
        var userId = GetCurrentUserId();

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
            return NotFound();

        return Ok(cartItem);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
