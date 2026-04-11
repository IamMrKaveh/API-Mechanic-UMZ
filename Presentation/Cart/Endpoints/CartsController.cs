using Application.Cart.Features.Commands.AddToCart;
using Application.Cart.Features.Commands.ClearCart;
using Application.Cart.Features.Commands.MergeGuestCart;
using Application.Cart.Features.Commands.RemoveFromCart;
using Application.Cart.Features.Commands.SyncCartPrices;
using Application.Cart.Features.Commands.UpdateCartItem;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Queries.GetCartSummary;
using Application.Cart.Features.Queries.ValidateCartForCheckout;
using Application.Cart.Features.Shared;
using MapsterMapper;

namespace Presentation.Cart.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class CartController(ISender mediator, IMapper mapper) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await Mediator.Send(new GetCartQuery(CurrentUser.UserId, GuestToken));
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetCartSummary()
    {
        var result = await Mediator.Send(new GetCartSummaryQuery(CurrentUser.UserId, CurrentUser.GuestToken));
        return ToActionResult(result);
    }

    [HttpGet("validate-checkout")]
    [Authorize]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var result = await Mediator.Send(
            new ValidateCartForCheckoutQuery(CurrentUser.UserId, CurrentUser.GuestToken));
        return ToActionResult(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
    {
        var command = new AddToCartCommand(CurrentUser.UserId, dto.VariantId, GuestToken, dto.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId}")]
    public async Task<IActionResult> UpdateItemQuantity(Guid variantId, [FromBody] UpdateCartItemDto dto)
    {
        var command = new UpdateCartItemCommand(CurrentUser.UserId, GuestToken, variantId, dto.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId}")]
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
        var result = await Mediator.Send(new MergeGuestCartCommand(GuestToken!));
        return ToActionResult(result);
    }

    [HttpPost("sync-prices")]
    [Authorize]
    public async Task<IActionResult> SyncCartPrices()
    {
        var command = new SyncCartPricesCommand(CurrentUser.UserId, CurrentUser.GuestToken ?? "");
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}