namespace MainApi.Shipping.Controllers;

[ApiController]
[Route("api/admin/shipping")]
[Authorize(Roles = "Admin")]
public class AdminShippingsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminShippingsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippings([FromQuery] bool includeDeleted = false)
    {
        var query = new GetShippingsQuery(includeDeleted);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingById(int id)
    {
        var result = await _mediator.Send(new GetShippingByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipping([FromBody] CreateShippingCommand command)
    {
        if (command.CurrentUserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { CurrentUserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetShippingById), new { id = result.Data!.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShipping(int id, [FromBody] UpdateShippingCommand command)
    {
        if (id != command.Id) return BadRequest("ID Mismatch");

        if (command.CurrentUserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { CurrentUserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShipping(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new DeleteShippingCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShipping(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new RestoreShippingCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}