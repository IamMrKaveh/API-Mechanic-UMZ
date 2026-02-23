namespace MainApi.Cart.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return result.IsSucceed ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var result = await _mediator.Send(new GetCartSummaryQuery());
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var result = await _mediator.Send(new ValidateCartForCheckoutQuery());
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPut("items/{variantId}")]
    public async Task<IActionResult> UpdateItemQuantity(int variantId, [FromBody] UpdateCartItemQuantityCommand command)
    {
        if (variantId != command.VariantId) return BadRequest("Mismatch VariantId");

        var result = await _mediator.Send(command);
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("items/{variantId}")]
    public async Task<IActionResult> RemoveItem(int variantId)
    {
        var result = await _mediator.Send(new RemoveFromCartCommand(variantId));
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand());
        return result.IsSucceed ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeCart([FromBody] MergeGuestCartCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSucceed ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var result = await _mediator.Send(new SyncCartPricesCommand());
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }
}