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
        return await Send(new GetUsersQuery(includeDeleted, page, pageSize), ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        return await Send(new GetUserByIdQuery(id), ct);
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

        return await Send(command, ct);
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreUser(Guid id, CancellationToken ct)
    {
        return await Send(new RestoreUserCommand(id), ct);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        return await Send(new UpdateUserCommand(id, request.FirstName, request.LastName), ct);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        return await Send(new DeleteUserCommand(id), ct);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequest request,
        CancellationToken ct)
    {
        return await Send(new ChangeUserStatusCommand(id, request.IsActive), ct);
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken ct)
    {
        return await Send(new ChangeUserRoleCommand(id, request.IsAdmin), ct);
    }
}