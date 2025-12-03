namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CartsController> _logger;

    public CartsController(
        ICartService cartService,
        ICurrentUserService currentUserService,
        ILogger<CartsController> logger)
    {
        _cartService = cartService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            guestId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Guest-Token", guestId);
        }

        var cart = await _cartService.GetCartAsync(userId, guestId);

        if (cart == null)
        {
            if (userId.HasValue)
            {
                var newCart = await _cartService.CreateCartAsync(userId.Value);
                return Ok(newCart);
            }

            return Ok(new CartDto(0, null, guestId, new List<CartItemDto>(), 0, 0, new List<CartPriceChangeDto>()));
        }

        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItemToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            guestId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Guest-Token", guestId);
        }

        var (result, cart) = await _cartService.AddItemToCartAsync(userId, guestId, dto);

        return result switch
        {
            CartOperationResult.Success => Ok(cart),
            CartOperationResult.NotFound => NotFound(new { message = "Product variant not found." }),
            CartOperationResult.OutOfStock => Conflict(new { message = "Failed to add item. Stock may have changed or item is unavailable." }),
            CartOperationResult.ConcurrencyConflict => Conflict(new { message = "Cart was updated by another process. Please refresh and try again.", cart }),
            _ => StatusCode(500, new { message = "An unexpected error occurred." })
        };
    }

    [HttpPut("items/{itemId}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var (result, cart) = await _cartService.UpdateCartItemAsync(userId, guestId, itemId, dto);

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
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var (success, cart) = await _cartService.RemoveItemFromCartAsync(userId, guestId, itemId);
        if (!success)
            return NotFound("Cart item not found or could not be removed.");

        return Ok(cart);
    }

    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

        var success = await _cartService.ClearCartAsync(userId, guestId);
        if (!success)
        {
            return StatusCode(500, "An error occurred while clearing the cart.");
        }
        return NoContent();
    }

    [HttpGet("items/count")]
    public async Task<ActionResult<int>> GetCartItemsCount()
    {
        var userId = _currentUserService.UserId;
        var guestId = _currentUserService.GuestId;

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Ok(0);

        var count = await _cartService.GetCartItemsCountAsync(userId, guestId);
        return Ok(count);
    }
}