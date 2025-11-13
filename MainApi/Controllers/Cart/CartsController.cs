namespace MainApi.Controllers.Cart;
[Route("api/[controller]")]
[ApiController]
public class CartsController : BaseApiController
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartsController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartsController(
        ICartService cartService,
        ILogger<CartsController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _cartService = cartService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? GetGuestId()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["X-Guest-Token"].FirstOrDefault();
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
        {
            return Ok(null);
        }

        var cart = await _cartService.GetCartAsync(userId, guestId);
        if (cart == null && userId.HasValue)
        {
            var newCart = await _cartService.CreateCartAsync(userId.Value);
            return Ok(newCart);
        }

        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItemToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Unauthorized("A user or guest token is required.");

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

        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

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
        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

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
        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

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
        var userId = GetCurrentUserId();
        var guestId = GetGuestId();

        if (!userId.HasValue && string.IsNullOrEmpty(guestId))
            return Ok(0);

        var count = await _cartService.GetCartItemsCountAsync(userId, guestId);
        return Ok(count);
    }
}