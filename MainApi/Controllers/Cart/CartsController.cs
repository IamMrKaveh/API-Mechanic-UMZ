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
            // If cart not found, create one for the user
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
    public async Task<ActionResult> AddItemToCart([FromBody] AddToCartDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");

        var result = await _cartService.AddItemToCartAsync(userId.Value, dto);

        return result switch
        {
            CartOperationResult.Success => Ok(new { Message = "Item added to cart successfully" }),
            CartOperationResult.NotFound => NotFound("Product not found."),
            CartOperationResult.OutOfStock => Conflict("Failed to add item. Stock may have changed or item is unavailable."),
            CartOperationResult.OptionsRequired => BadRequest("Color and Size are required for this product."),
            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }

    [HttpPut("items/{itemId}")]
    public async Task<ActionResult> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");

        var result = await _cartService.UpdateCartItemAsync(userId.Value, itemId, dto);
        if (!result)
            return Conflict("Failed to update item. Stock may have changed or item not found.");

        return Ok(new { Message = "Cart item updated successfully" });
    }

    [HttpDelete("items/{itemId}")]
    public async Task<ActionResult> RemoveItemFromCart(int itemId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized("Invalid user");

        var result = await _cartService.RemoveItemFromCartAsync(userId.Value, itemId);
        if (!result)
            return NotFound("Cart item not found or could not be removed.");

        return Ok(new { Message = "Item removed from cart successfully" });
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