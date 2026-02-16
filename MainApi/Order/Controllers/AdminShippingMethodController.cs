namespace MainApi.Order.Controllers;

[ApiController]
[Route("api/admin/shipping-methods")]
[Authorize(Roles = "Admin")]
public class AdminShippingMethodsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminShippingMethodsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods([FromQuery] bool includeDeleted = false)
    {
        var query = new GetShippingMethodsQuery(includeDeleted);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingMethodById(int id)
    {
        var result = await _mediator.Send(new GetShippingMethodByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShippingMethod([FromBody] CreateShippingMethodCommand command)
    {
        if (command.CurrentUserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { CurrentUserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetShippingMethodById), new { id = result.Data!.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShippingMethod(int id, [FromBody] UpdateShippingMethodCommand command)
    {
        if (id != command.Id) return BadRequest("ID Mismatch");

        if (command.CurrentUserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { CurrentUserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShippingMethod(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new DeleteShippingMethodCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShippingMethod(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new RestoreShippingMethodCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}