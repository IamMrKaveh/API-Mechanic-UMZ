namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartItemsController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartItemsController> _logger;

    public CartItemsController(ICartService cartService, ILogger<CartItemsController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized("Invalid user");

        var cart = await _cartService.GetCartByUserIdAsync(userId);
        if (cart == null)
        {
            return StatusCode(500, "Could not retrieve cart.");
        }
        return Ok(cart);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCartItem([FromBody] AddToCartDto addToCartDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized("Invalid user");

        var success = await _cartService.AddItemToCartAsync(userId, addToCartDto);
        if (!success)
        {
            return Conflict("Failed to add item. Stock may have changed or item is unavailable.");
        }

        return Ok(new { message = "Item added to cart successfully." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemDto updateCartItemDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized("Invalid user");

        var success = await _cartService.UpdateCartItemAsync(userId, id, updateCartItemDto);
        if (!success)
        {
            return Conflict("Failed to update item. Stock may have changed or item not found.");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCartItem(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized("Invalid user");

        var success = await _cartService.RemoveItemFromCartAsync(userId, id);
        if (!success)
        {
            return NotFound("Cart item not found or could not be removed.");
        }

        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized("Invalid user");

        var success = await _cartService.ClearCartAsync(userId);
        if (!success)
        {
            return StatusCode(500, "An error occurred while clearing the cart.");
        }

        return NoContent();
    }

    [NonAction]
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return 0;
        return userId;
    }
}