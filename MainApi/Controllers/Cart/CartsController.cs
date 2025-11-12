namespace MainApi.Controllers.Cart;
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartsController : BaseApiController
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartsController> _logger;
    public CartsController(
        ICartService cartService,
        ILogger<CartsController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var cart = await _cartService.GetCartByUserIdAsync(userId.Value);
        if (cart == null)
        {
            var newCart = await _cartService.CreateCartAsync(userId.Value);
            if (newCart == null)
            {
                return StatusCode(500, "Could not create or retrieve a cart for the user.");
            }
            return Ok(newCart);
        }
        return Ok(cart);
    }

    [HttpPost]
    public async Task<ActionResult<CartDto>> CreateCart()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var existingCart = await _cartService.GetCartByUserIdAsync(userId.Value);
        if (existingCart != null)
        {
            return Ok(existingCart);
        }

        var newCart = await _cartService.CreateCartAsync(userId.Value);
        if (newCart == null)
        {
            return StatusCode(500, "Could not create a cart for the user.");
        }

        return CreatedAtAction(nameof(GetMyCart), newCart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItemToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var (result, cart) = await _cartService.AddItemToCartAsync(userId.Value, dto);

        return result switch
        {
            CartOperationResult.Success => Ok(cart),
            CartOperationResult.NotFound => NotFound(new { message = "Product variant not found." }),
            CartOperationResult.OutOfStock => Conflict(new { message = "Failed to add item. Stock may have changed or item is unavailable." }),
            _ => StatusCode(500, new { message = "An unexpected error occurred." })
        };
    }

    [HttpPut("items/{itemId}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var (result, cart) = await _cartService.UpdateCartItemAsync(userId.Value, itemId, dto);

        return result switch
        {
            CartOperationResult.Success => Ok(cart),
            CartOperationResult.NotFound => NotFound(new { message = "Cart item not found." }),
            CartOperationResult.OutOfStock => Conflict(new { message = "Insufficient product stock." }),
            CartOperationResult.ConcurrencyConflict => Conflict(new { message = "Cart was updated by another process. Please refresh and try again.", cart }),
            _ => StatusCode(500, new { message = "An unexpected error occurred while updating the cart." })
        };
    }

    [HttpDelete("items/{itemId}")]
    public async Task<ActionResult<CartDto>> RemoveItemFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var (success, cart) = await _cartService.RemoveItemFromCartAsync(userId.Value, itemId);
        if (!success)
            return NotFound("Cart item not found or could not be removed.");
        return Ok(cart);
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var success = await _cartService.ClearCartAsync(userId.Value);
        if (!success)
        {
            return StatusCode(500, "An error occurred while clearing the cart.");
        }
        return NoContent();
    }

    [HttpGet("items/count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");
        var count = await _cartService.GetCartItemsCountAsync(userId.Value);
        return Ok(count);
    }
}