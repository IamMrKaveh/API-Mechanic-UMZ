namespace MainApi.Controllers.Admin;

[Route("api/admin/users")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly ICurrentUserService _currentUserService;

    public AdminUsersController(IAdminUserService adminUserService, ICurrentUserService currentUserService)
    {
        _adminUserService = adminUserService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminUserService.GetUsersAsync(includeDeleted, page, pageSize);
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving users" });
        return Ok(result.Data);
    }


    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] Domain.User.User tUsers)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _adminUserService.CreateUserAsync(tUsers);
        if (!result.Success)
        {
            return result.Data.Error == "User with this phone number already exists."
                ? Conflict(result.Data.Error)
                : BadRequest(result.Data.Error);
        }

        var userResult = await _adminUserService.GetUserByIdAsync(result.Data.User!.Id);

        return CreatedAtAction(nameof(GetUser), new { id = result.Data.User?.Id }, userResult.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _adminUserService.GetUserByIdAsync(id);
        if (!result.Success) return NotFound(new { Message = result.Error });
        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();

        var result = await _adminUserService.UpdateUserAsync(id, updateRequest, currentUserId.Value);
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
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeUserStatusDto dto)
    {
        var result = await _adminUserService.ChangeUserStatusAsync(id, dto.IsActive);
        if (result.Success) return NoContent();
        return result.Error == "NotFound" ? NotFound() : StatusCode(500, "An error occurred");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();

        var result = await _adminUserService.DeleteUserAsync(id, currentUserId.Value);
        if (result.Success) return NoContent();

        return result.Error switch
        {
            "NotFound" => NotFound(),
            "Admins cannot delete their own account this way." => BadRequest(result.Error),
            _ => StatusCode(500, "An error occurred")
        };
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUser(int id)
    {
        var result = await _adminUserService.RestoreUserAsync(id);
        if (result.Success) return NoContent();
        return result.Error == "NotFound" ? NotFound("User not found or not deleted.") : BadRequest(result.Error);
    }
}