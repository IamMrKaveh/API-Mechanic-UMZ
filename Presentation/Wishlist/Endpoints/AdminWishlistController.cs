using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;

namespace Presentation.Wishlist.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/wishlist")]
[Authorize(Roles = "Admin")]
public sealed class AdminWishlistController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("{userId:guid}/wishlist")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WishlistItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserWishlist(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetWishlistByIdQuery(userId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}