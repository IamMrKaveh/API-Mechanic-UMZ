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
        var query = new GetWishlistByIdQuery(CurrentUser.UserId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsInWishlist(Guid productId, CancellationToken ct)
    {
        var query = new CheckWishlistStatusQuery(CurrentUser.UserId, productId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost()]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ToggleWishlist(
        [FromBody] ToggleWishlistRequest request,
        CancellationToken ct)
    {
        var command = new ToggleWishlistCommand(request.ProductId, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}