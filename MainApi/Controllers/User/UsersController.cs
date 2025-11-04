using MainApi.Services.User;

namespace MainApi.Controllers.User;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers([FromQuery] bool includeDeleted = false)
    {
        try
        {
            var users = await _userService.GetUsersAsync(includeDeleted);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users");
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        if (currentUserId != id && !User.IsInRole("Admin"))
            return Forbid();

        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, "An error occurred while retrieving user");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserProfileDto>> CreateUser([FromBody] TUsers tUsers)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var (success, user, error) = await _userService.CreateUserAsync(tUsers);
            if (!success)
            {
                return error == "User with this phone number already exists."
                    ? Conflict(error)
                    : BadRequest(error);
            }
            return CreatedAtAction(nameof(GetUser), new { id = user!.Id }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating user");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        try
        {
            var (success, error) = await _userService.UpdateUserAsync(id, updateRequest, currentUserId.Value, User.IsInRole("Admin"));
            if (success) return NoContent();

            return error switch
            {
                "Forbidden" => Forbid(),
                "NotFound" => NotFound(),
                "User account is deleted and cannot be modified." => Forbid(error),
                "User was modified by another process" => Conflict(error),
                _ => StatusCode(500, "An error occurred while updating user")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "An error occurred while updating user");
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] bool isActive)
    {
        try
        {
            var (success, error) = await _userService.ChangeUserStatusAsync(id, isActive);
            if (success) return NoContent();

            return error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user status {UserId}", id);
            return StatusCode(500, "An error occurred while changing user status");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized();

        try
        {
            var (success, error) = await _userService.DeleteUserAsync(id, currentUserId.Value);
            if (success) return NoContent();

            return error switch
            {
                "NotFound" => NotFound(),
                "Admins cannot delete their own account this way." => BadRequest(error),
                _ => StatusCode(500, "An error occurred")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "An error occurred while deleting user");
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            var (success, message, otp) = await _userService.LoginAsync(request, clientIp);
            if (!success)
            {
                if (message != null && message.StartsWith("Too many"))
                    return StatusCode(429, message);
                return Unauthorized(message);
            }
            //var apiKey = _configuration["Kavenegar:ApiKey"];
            //var template = "verify";
            //if (string.IsNullOrEmpty(apiKey))
            //    throw new InvalidOperationException("SMS service is not configured.");
            //var api = new KavenegarApi(apiKey);
            //await Task.Run(() => api.VerifyLookup(request.PhoneNumber, otp, template));
            return Ok(new { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process for phone: {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, "Failed to send SMS verification code.");
        }
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var (response, error) = await _userService.VerifyOtpAsync(request, clientIp, userAgent);

        if (response != null)
            return Ok(response);

        if (error != null && error.StartsWith("Too many"))
            return StatusCode(429, error);

        return BadRequest(error);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var (response, error) = await _userService.RefreshTokenAsync(request, clientIp, userAgent);

        if (response != null)
            return Ok(response);

        return Unauthorized(new { message = error });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        var (success, error) = await _userService.LogoutAsync(request.RefreshToken);

        if (success)
            return Ok(new { message = error });

        return error == "Active token not found." ? NotFound(new { message = error }) : BadRequest(new { message = error });
    }
}