using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetUserById;
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class UserController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateProfileRequest request)
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
}