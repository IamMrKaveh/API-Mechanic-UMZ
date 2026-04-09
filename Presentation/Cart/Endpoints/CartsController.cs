using Application.Cart.Features.Commands.AddToCart;
using Application.Cart.Features.Commands.ClearCart;
using Application.Cart.Features.Commands.MergeGuestCart;
using Application.Cart.Features.Commands.RemoveFromCart;
using Application.Cart.Features.Commands.SyncCartPrices;
using Application.Cart.Features.Commands.UpdateCartItem;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Queries.GetCartSummary;
using Application.Cart.Features.Queries.ValidateCartForCheckout;
using Presentation.Cart.Requests;

namespace Presentation.Cart.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class CartController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await Mediator.Send(new GetCartQuery(CurrentUser.UserId, GuestId));
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var result = await Mediator.Send(new GetCartSummaryQuery());
        return ToActionResult(result);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var result = await Mediator.Send(new ValidateCartForCheckoutQuery());
        return ToActionResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        var command = new AddToCartCommand(
            CurrentUser.UserId,
            request.VariantId,
            GuestId,
            request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId}")]
    public async Task<IActionResult> UpdateItemQuantity(
        Guid variantId,
        [FromBody] UpdateCartItemRequest request)
    {
        var command = new UpdateCartItemCommand(CurrentUser.UserId, GuestId, variantId, request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId}")]
    public async Task<IActionResult> RemoveItem(Guid variantId)
    {
        var command = new RemoveFromCartCommand(CurrentUser.UserId, GuestId, variantId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var command = new ClearCartCommand(CurrentUser.UserId, GuestId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeCart()
    {
        var result = await Mediator.Send(new MergeGuestCartCommand(GuestId!));
        return ToActionResult(result);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var result = await Mediator.Send(new SyncCartPricesCommand());
        return ToActionResult(result);
    }
}