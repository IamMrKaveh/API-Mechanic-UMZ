namespace MainApi.Cart.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery());
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var result = await _mediator.Send(new GetCartSummaryQuery());
        return ToActionResult(result);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var result = await _mediator.Send(new ValidateCartForCheckoutQuery());
        return ToActionResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId}")]
    public async Task<IActionResult> UpdateItemQuantity(int variantId, [FromBody] UpdateCartItemQuantityCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId}")]
    public async Task<IActionResult> RemoveItem(int variantId)
    {
        var result = await _mediator.Send(new RemoveFromCartCommand(variantId));
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var result = await _mediator.Send(new ClearCartCommand());
        return ToActionResult(result);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeCart([FromBody] MergeGuestCartCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var result = await _mediator.Send(new SyncCartPricesCommand());
        return ToActionResult(result);
    }
}