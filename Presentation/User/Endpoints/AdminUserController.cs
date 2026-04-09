using Application.User.Features.Commands.ChangeUserRole;
using Application.User.Features.Commands.ChangeUserStatus;
using Application.User.Features.Commands.CreateUser;
using Application.User.Features.Commands.DeleteUser;
using Application.User.Features.Commands.RestoreUser;
using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetUserById;
using Application.User.Features.Queries.GetUsers;
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

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
        var result = await _mediator.Send(new GetUsersQuery(includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        var command = new CreateUserCommand(
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.IsAdmin);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateProfileRequest request)
    {
        var command = new UpdateUserCommand(
            id,
            CurrentUser.UserId,
            request.FirstName,
            request.LastName,
            request.Email);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeUserStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequest request)
    {
        var result = await _mediator.Send(new ChangeUserStatusCommand(id, request.IsActive));
        return ToActionResult(result);
    }

    [HttpPatch("{id}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request)
    {
        var result = await _mediator.Send(
            new ChangeUserRoleCommand(id, request.IsAdmin, CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id, CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id)
    {
        var result = await _mediator.Send(new RestoreUserCommand(id));
        return ToActionResult(result);
    }
}