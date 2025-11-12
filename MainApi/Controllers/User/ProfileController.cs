using MainApi.Services.User;

namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _userService.GetUserProfileAsync(userId.Value);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var (success, error) = await _userService.UpdateProfileAsync(userId.Value, updateRequest);
            if (success) return NoContent();

            return error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return StatusCode(500, "An error occurred while updating the profile.");
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var (success, error) = await _userService.DeleteAccountAsync(userId.Value);
            if (success) return Ok(new { message = "Account successfully deleted." });

            return error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
            return StatusCode(500, "An error occurred while deleting the account.");
        }
    }

    [HttpGet("reviews")]
    public async Task<ActionResult<IEnumerable<ProductReviewDto>>> GetMyReviews()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var reviews = await _userService.GetUserReviewsAsync(userId.Value);
        return Ok(reviews);
    }
}