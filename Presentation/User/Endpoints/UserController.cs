using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetUserById;
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[Route("api/users")]
[ApiController]
[Authorize]
public sealed class UserController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), ct);
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
}