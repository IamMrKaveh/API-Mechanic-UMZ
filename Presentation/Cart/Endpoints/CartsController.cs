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
public sealed class CartController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var query = new GetCartQuery(CurrentUser.UserId, GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var query = new GetCartSummaryQuery(CurrentUser.UserId, CurrentUser.GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var query = new ValidateCartForCheckoutQuery(CurrentUser.UserId, CurrentUser.GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        var command = new AddToCartCommand(CurrentUser.UserId, request.VariantId, GuestToken, request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId:guid}")]
    public async Task<IActionResult> UpdateItemQuantity(Guid variantId, [FromBody] UpdateCartItemRequest request)
    {
        var command = new UpdateCartItemCommand(CurrentUser.UserId, GuestToken, variantId, request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid variantId)
    {
        var command = new RemoveFromCartCommand(CurrentUser.UserId, GuestToken, variantId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var command = new ClearCartCommand(CurrentUser.UserId, GuestToken);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("merge")]
    [Authorize]
    public async Task<IActionResult> MergeCart()
    {
        var command = new MergeGuestCartCommand(GuestToken!);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var command = new SyncCartPricesCommand(CurrentUser.UserId, CurrentUser.GuestToken ?? string.Empty);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}