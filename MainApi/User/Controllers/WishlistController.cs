using Application.Wishlist.Features.Shared;

namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetWishlistByIdQuery(CurrentUser.UserId, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistDto dto)
    {
        var command = new ToggleWishlistCommand(dto.ProductId, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> IsInWishlist(int productId)
    {
        var query = new CheckWishlistStatusQuery(CurrentUser.UserId, productId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}