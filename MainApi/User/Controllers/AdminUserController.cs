namespace MainApi.User.Controllers;

[Route("api/admin/user")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUserController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminUserController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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

        if (result.IsSucceed)
            return CreatedAtAction(nameof(GetUser), new { id = result.Data?.Id }, result.Data);

        return ToActionResult(ServiceResult.Failure(result.Error ?? "Error"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var query = new GetAdminUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new UpdateUserCommand(id, updateRequest, CurrentUser.UserId.Value);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new DeleteUserCommand(id, CurrentUser.UserId.Value);
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