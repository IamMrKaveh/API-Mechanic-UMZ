using Application.Common.Interfaces.User;
using MainApi.Controllers.Base;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistController : BaseApiController
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist()
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _wishlistService.GetUserWishlistAsync(userId.Value);
        return ToActionResult(result);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist([FromBody] ToggleWishlistDto dto)
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _wishlistService.ToggleWishlistAsync(userId.Value, dto.ProductId);
        return ToActionResult(result);
    }

    [HttpGet("check/{productId}")]
    public async Task<IActionResult> IsInWishlist(int productId)
    {
        var userId = CurrentUser.UserId;
        if (userId == null) return Unauthorized();

        var result = await _wishlistService.IsInWishlistAsync(userId.Value, productId);
        return ToActionResult(result);
    }
}