using Application.Wishlist.Features.Commands.ToggleWishlist;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Presentation.Wishlist.Requests;

namespace Presentation.Wishlist.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(
            new GetWishlistByIdQuery(CurrentUser.UserId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistRequest request)
    {
        var command = new ToggleWishlistCommand(request.ProductId, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> IsInWishlist(Guid productId)
    {
        var result = await _mediator.Send(
            new CheckWishlistStatusQuery(CurrentUser.UserId, productId));
        return ToActionResult(result);
    }
}