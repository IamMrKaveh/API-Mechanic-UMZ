namespace MainApi.Order.Controllers;

[Route("api/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController(IMediator mediator, ICurrentUserService currentUserService) : BaseApiController(currentUserService)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatuses()
    {
        var query = new GetOrderStatusesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderStatus([FromBody] CreateOrderStatusCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess) return CreatedAtAction(nameof(GetOrderStatus), new { id = result.Value!.Id }, result.Value);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        var result = await _mediator.Send(new GetOrderStatusByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _mediator.Send(new UpdateOrderStatusDefinitionCommand(id, dto));
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderStatus(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var command = new DeleteOrderStatusCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}