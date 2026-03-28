namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        var command = new UpdateUserCommand(id, updateRequest, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}