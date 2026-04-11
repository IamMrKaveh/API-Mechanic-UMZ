using Application.Wishlist.Features.Commands.ToggleWishlist;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;

namespace Presentation.Wishlist.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetWishlistByIdQuery(CurrentUser.UserId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistDto dto)
    {
        var command = new ToggleWishlistCommand(dto.ProductId, CurrentUser.UserId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> IsInWishlist(Guid productId)
    {
        var result = await Mediator.Send(new CheckWishlistStatusQuery(CurrentUser.UserId, productId));
        return ToActionResult(result);
    }
}