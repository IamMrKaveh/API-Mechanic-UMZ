namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(MechanicContext context, ILogger<ProfileController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _context.TUsers
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                CreatedAt = u.CreatedAt,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .FirstOrDefaultAsync();

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

        var existingUser = await _context.TUsers.FindAsync(userId);
        if (existingUser == null || existingUser.IsDeleted)
            return NotFound();

        existingUser.FirstName = updateRequest.FirstName;
        existingUser.LastName = updateRequest.LastName;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
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
        {
            return Unauthorized();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.TUsers.FindAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsActive = false;

            await RevokeAllUserRefreshTokensAsync(userId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Account successfully deleted." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
            return StatusCode(500, "An error occurred while deleting the account.");
        }
    }

    private async Task RevokeAllUserRefreshTokensAsync(int? userId)
    {
        if (userId != null)
        {
            var userTokens = _context.TRefreshToken
            .Where(
                rt => rt.UserId == userId &&
                rt.RevokedAt == null &&
                rt.ExpiresAt > DateTime.UtcNow);

            await userTokens.ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
        }
    }
}