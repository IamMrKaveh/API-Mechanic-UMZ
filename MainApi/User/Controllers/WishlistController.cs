namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController : BaseApiController
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserWishlistQuery(CurrentUser.UserId.Value, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new ToggleWishlistCommand(dto.ProductId, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> IsInWishlist(int productId)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new CheckWishlistStatusQuery(CurrentUser.UserId.Value, productId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}