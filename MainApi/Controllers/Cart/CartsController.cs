namespace MainApi.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet("my-cart")]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        var cart = await _cartService.GetCartByUserIdAsync(userId);
        return Ok(cart);
    }

    [HttpPost("add-item")]
    public async Task<ActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var result = await _cartService.AddItemToCartAsync(userId, dto);
        if (!result)
            return BadRequest("Unable to add item to cart. Check product availability or stock.");
        return Ok(new { Message = "Item added to cart successfully" });
    }

    [HttpPut("update-item/{itemId}")]
    public async Task<ActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var result = await _cartService.UpdateCartItemAsync(userId, itemId, dto);
        if (!result)
            return BadRequest("Unable to update cart item. Check quantity or item existence.");
        return Ok(new { Message = "Cart item updated successfully" });
    }

    [HttpDelete("remove-item/{itemId}")]
    public async Task<ActionResult> RemoveFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var result = await _cartService.RemoveItemFromCartAsync(userId, itemId);
        if (!result)
            return NotFound("Cart item not found");
        return Ok(new { Message = "Item removed from cart successfully" });
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");

        await _cartService.ClearCartAsync(userId);
        return Ok(new { Message = "Cart cleared successfully" });
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized("Invalid user");
        var count = await _cartService.GetCartItemsCountAsync(userId);
        return Ok(count);
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