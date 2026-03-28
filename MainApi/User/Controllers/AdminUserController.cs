namespace MainApi.User.Controllers;

[Route("api/admin/user")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUserController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetUsersQuery(includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserDto dto)
    {
        var command = new CreateUserCommand(dto);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        var command = new UpdateUserCommand(id, updateRequest, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeUserStatusDto dto)
    {
        var command = new ChangeUserStatusCommand(id, dto.IsActive);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeUserRoleRequest dto)
    {
        var command = new ChangeUserRoleCommand(id, dto.IsAdmin, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var command = new DeleteUserCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUser(int id)
    {
        var command = new RestoreUserCommand(id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}