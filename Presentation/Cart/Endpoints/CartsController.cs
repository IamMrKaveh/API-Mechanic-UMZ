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
using Presentation.Cart.Requests;

namespace Presentation.Cart.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/cart")]
public sealed class CartController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var query = new GetCartQuery(CurrentUser.UserId, GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<CartSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartSummary()
    {
        var query = new GetCartSummaryQuery(CurrentUser.UserId, CurrentUser.GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("checkout/validation")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartCheckoutValidationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCartForCheckout()
    {
        var query = new ValidateCartForCheckoutQuery(CurrentUser.UserId, CurrentUser.GuestToken);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        var command = new AddToCartCommand(CurrentUser.UserId, request.VariantId, GuestToken, request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("merge")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> MergeCart()
    {
        var command = new MergeGuestCartCommand(
            CurrentUser.UserId,
            GuestToken);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("prices")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> SyncCartPrices()
    {
        var command = new SyncCartPricesCommand(CurrentUser.UserId, CurrentUser.GuestToken ?? string.Empty);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItemQuantity(Guid variantId, [FromBody] UpdateCartItemRequest request)
    {
        var command = new UpdateCartItemCommand(CurrentUser.UserId, GuestToken, variantId, request.Quantity);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearCart()
    {
        var command = new ClearCartCommand(CurrentUser.UserId, GuestToken);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid variantId)
    {
        var command = new RemoveFromCartCommand(CurrentUser.UserId, GuestToken, variantId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}