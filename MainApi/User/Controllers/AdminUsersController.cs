using Application.User.Features.Queries.GetUsers;

namespace MainApi.User.Controllers;

[Route("api/admin/users")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator, ICurrentUserService currentUserService)
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
        var query = new GetAdminUsersQuery(includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] Domain.User.User user)
    {
        // توجه: معمولا DTO پاس می‌دهیم نه Entity. اینجا برای سازگاری با کد قبلی است.
        var command = new CreateUserCommand(user);
        var result = await _mediator.Send(command);

        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetUser), new { id = result.Data.User?.Id }, result.Data.User);
        }
        return ToActionResult(ServiceResult.Failure(result.Data.Error ?? "Error"));
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