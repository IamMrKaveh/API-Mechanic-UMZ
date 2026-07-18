using Application.Cart.Features.Commands.AddItemToCart;
using Application.Cart.Features.Commands.ClearCart;
using Application.Cart.Features.Commands.MergeGuestCart;
using Application.Cart.Features.Commands.RemoveItemFromCart;
using Application.Cart.Features.Commands.SyncCartPrices;
using Application.Cart.Features.Commands.UpdateCartItemQuantity;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Queries.GetCartSummary;
using Application.Cart.Features.Queries.ValidateCartForCheckout;
using Application.Cart.Features.Shared;
using Presentation.Cart.Requests;

namespace Presentation.Cart.Endpoints;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
[AllowAnonymous]
public sealed class CartController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var query = new GetCartQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<CartSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartSummary(CancellationToken ct)
    {
        var query = new GetCartSummaryQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("checkout/validation")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartCheckoutValidationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCartForCheckout(CancellationToken ct)
    {
        var query = new ValidateCartForCheckoutQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request,
        CancellationToken ct)
    {
        var command = new AddItemToCartCommand(
            request.VariantId,
            request.Quantity);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("items/{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity(
        Guid variantId,
        [FromBody] UpdateCartItemQuantityRequest request,
        CancellationToken ct)
    {
        var command = new UpdateCartItemQuantityCommand(
            variantId,
            request.Quantity);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("items/{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        Guid variantId,
        CancellationToken ct)
    {
        var command = new RemoveItemFromCartCommand(variantId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        var command = new ClearCartCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result, StatusCodes.Status204NoContent);
    }

    [HttpPost("merge")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MergeCart(CancellationToken ct)
    {
        var command = new MergeGuestCartCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("prices")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CartDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SyncCartPrices(CancellationToken ct)
    {
        var command = new SyncCartPricesCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}