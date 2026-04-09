using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Application.Order.Features.Queries.GetOrderStatusById;
using Application.Order.Features.Queries.GetOrderStatuses;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

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
        var result = await _mediator.Send(new GetOrderStatusesQuery());
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderStatus(
        [FromBody] CreateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderStatusCommand(
            request.Name,
            request.DisplayName,
            null,
            null,
            request.SortOrder,
            request.AllowCancel,
            request.AllowEdit);

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(Guid id)
    {
        var result = await _mediator.Send(new GetOrderStatusByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusDefinitionCommand(
            id,
            request.Name,
            request.DisplayName,
            request.Description,
            request.SortOrder,
            request.IsDefault);

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderStatus(Guid id)
    {
        var command = new DeleteOrderStatusCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}