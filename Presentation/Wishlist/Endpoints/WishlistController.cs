using Application.Wishlist.Features.Commands.ToggleWishlist;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Presentation.Wishlist.Requests;

namespace Presentation.Wishlist.Endpoints;

[Route("api/wishlist")]
[ApiController]
[Authorize]
public sealed class WishlistController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(
            new GetWishlistByIdQuery(CurrentUser.UserId, page, pageSize), ct);
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist(
        [FromBody] ToggleWishlistRequest request,
        CancellationToken ct)
    {
        var command = new ToggleWishlistCommand(request.ProductId, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId:guid}")]
    public async Task<IActionResult> IsInWishlist(Guid productId, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new CheckWishlistStatusQuery(CurrentUser.UserId, productId), ct);
        return ToActionResult(result);
    }
}