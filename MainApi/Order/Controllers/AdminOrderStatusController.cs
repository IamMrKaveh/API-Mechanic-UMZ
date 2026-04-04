using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Application.Order.Features.Queries.GetOrderStatusById;
using Application.Order.Features.Queries.GetOrderStatuses;
using Application.Order.Features.Shared;

namespace MainApi.Order.Controllers;

[Route("api/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController(IMediator mediator) : BaseApiController(mediator)
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
        var command = new DeleteOrderStatusCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}