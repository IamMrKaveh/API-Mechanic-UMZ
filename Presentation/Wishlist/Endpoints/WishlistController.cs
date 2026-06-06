using Application.Wishlist.Features.Commands.ClearWishlist;
using Application.Wishlist.Features.Commands.RemoveFromWishlist;
using Application.Wishlist.Features.Commands.ToggleWishlist;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;
using Presentation.Wishlist.Requests;

namespace Presentation.Wishlist.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/wishlist")]
[Authorize]
public sealed class WishlistController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WishlistItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetWishlistByIdQuery(RequestContext.UserId ?? Guid.Empty, page, pageSize), ct);
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsInWishlist(Guid productId, CancellationToken ct)
    {
        return await Send(new CheckWishlistStatusQuery(RequestContext.UserId ?? Guid.Empty, productId), ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ToggleWishlist(
        [FromBody] ToggleWishlistRequest request,
        CancellationToken ct)
    {
        return await Send(new ToggleWishlistCommand(request.ProductId, RequestContext.UserId ?? Guid.Empty), ct);
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken ct)
    {
        var command = new RemoveFromWishlistCommand(RequestContext.UserId ?? Guid.Empty, productId);
        await Mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearWishlist(CancellationToken ct)
    {
        var command = new ClearWishlistCommand(RequestContext.UserId ?? Guid.Empty);
        await Mediator.Send(command, ct);
        return NoContent();
    }
}