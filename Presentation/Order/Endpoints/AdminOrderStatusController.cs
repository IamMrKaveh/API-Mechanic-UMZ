using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Queries.GetOrderStatuses;
using MapsterMapper;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[Route("api/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatuses(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderStatusesQuery(), ct);
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

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderStatusQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
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

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrderStatus(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderStatusCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}