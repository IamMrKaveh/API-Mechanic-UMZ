namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ILogger<UsersController> logger, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers([FromQuery] bool includeDeleted = false)
    {
        var result = await _userService.GetUsersAsync(includeDeleted);
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving users" });
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();
        if (currentUserId != id && !_currentUserService.IsAdmin) return Forbid();

        var result = await _userService.GetUserByIdAsync(id);
        if (!result.Success) return NotFound(new { Message = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] Domain.User.User tUsers)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(tUsers);
        if (!result.Success)
        {
            return result.Data.Error == "User with this phone number already exists."
                ? Conflict(result.Data.Error)
                : BadRequest(result.Data.Error);
        }
        return CreatedAtAction(nameof(GetUser), new { id = result.Data.User?.Id }, result.Data.User);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();

        var result = await _userService.UpdateUserAsync(id, updateRequest, currentUserId.Value, _currentUserService.IsAdmin);
        if (result.Success) return NoContent();

        return result.Error switch
        {
            "Forbidden" => Forbid(),
            "NotFound" => NotFound(),
            "User account is deleted and cannot be modified." => Forbid(result.Error),
            "User was modified by another process" => Conflict(result.Error),
            _ => StatusCode(500, "An error occurred while updating user")
        };
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeUserStatusDto dto)
    {
        var result = await _userService.ChangeUserStatusAsync(id, dto.IsActive);
        if (result.Success) return NoContent();
        return result.Error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();

        var result = await _userService.DeleteUserAsync(id, currentUserId.Value);
        if (result.Success) return NoContent();

        return result.Error switch
        {
            "NotFound" => NotFound(),
            "Admins cannot delete their own account this way." => BadRequest(result.Error),
            _ => StatusCode(500, "An error occurred")
        };
    }

    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RestoreUser(int id)
    {
        var result = await _userService.RestoreUserAsync(id);
        if (result.Success) return NoContent();
        return result.Error == "NotFound" ? NotFound("User not found or not deleted.") : BadRequest(result.Error);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _userService.LoginAsync(request, clientIp);

        if (!result.Success || result.Data.Otp == null)
        {
            if (result.Error != null && result.Error.StartsWith("Too many")) return StatusCode(429, result.Error);
            return Unauthorized(result.Error);
        }
        return Ok(new { result.Data.Message });
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _userService.VerifyOtpAsync(request, clientIp, userAgent);

        if (result.Success && result.Data.Response != null) return Ok(result.Data.Response);

        if (result.Data.Error != null && result.Data.Error.StartsWith("Too many")) return StatusCode(429, result.Data.Error);
        return BadRequest(result.Data.Error);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _userService.RefreshTokenAsync(request, clientIp, userAgent);

        if (result.Success && result.Data.Response != null) return Ok(result.Data.Response);
        return Unauthorized(new { message = result.Data.Error });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        var result = await _userService.LogoutAsync(request.refreshToken);
        if (result.Success) return Ok(new { message = "Logged out successfully." });
        return BadRequest(new { message = result.Error });
    }
}