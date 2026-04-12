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
public sealed class AdminUserController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetUsersQuery(includeDeleted, page, pageSize), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] AdminCreateUserRequest request,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.IsAdmin);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            id,
            CurrentUser.UserId,
            request.FirstName,
            request.LastName);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeUserStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new ChangeUserStatusCommand(id, request.IsActive), ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(
            new ChangeUserRoleCommand(id, request.IsAdmin, CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteUserCommand(id, CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new RestoreUserCommand(id), ct);
        return ToActionResult(result);
    }
}