using Application.Common.Interfaces.User;
using Application.DTOs.Auth;

namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
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

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _userService.LoginAsync(request, clientIp);

        if (!result.Success)
        {
            if (result.Error != null && result.Error.StartsWith("Too many")) return StatusCode(429, result.Error);
            return Unauthorized(result.Error);
        }
        return Ok(new { Message = result.Data });
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
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
    [IgnoreAntiforgeryToken]
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