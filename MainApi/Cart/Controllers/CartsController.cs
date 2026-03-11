namespace MainApi.Cart.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var result = await _mediator.Send(new GetCartSummaryQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var result = await _mediator.Send(new ValidateCartForCheckoutQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("items/{variantId}")]
    public async Task<IActionResult> UpdateItemQuantity(int variantId, [FromBody] UpdateCartItemQuantityCommand command)
    {
        if (variantId != command.VariantId) return BadRequest("Mismatch VariantId");

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("items/{variantId}")]
    public async Task<IActionResult> RemoveItem(int variantId)
    {
        var result = await _mediator.Send(new RemoveFromCartCommand(variantId));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand());
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeCart([FromBody] MergeGuestCartCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var result = await _mediator.Send(new SyncCartPricesCommand());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}