using Application.User.Features.Commands.ChangeUserRole;
using Application.User.Features.Commands.ChangeUserStatus;
using Application.User.Features.Commands.CreateUser;
using Application.User.Features.Commands.DeleteUser;
using Application.User.Features.Commands.RestoreUser;
using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetUserById;
using Application.User.Features.Queries.GetUsers;
using Application.User.Features.Shared;
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUserController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<UserProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetUsersQuery(includeDeleted, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var command = new GetUserByIdQuery(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status201Created)]
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

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreUser(Guid id, CancellationToken ct)
    {
        var command = new RestoreUserCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var command = new DeleteUserCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken ct)
    {
        var command = new ChangeUserStatusCommand(id, request.IsActive);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken ct)
    {
        var command = new ChangeUserRoleCommand(id, request.IsAdmin, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}