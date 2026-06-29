using Application.Order.Features.Commands.ActivateOrderStatus;
using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeactivateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
using Application.Order.Features.Commands.SetDefaultOrderStatus;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Queries.GetOrderStatuses;
using Application.Order.Features.Shared;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[Route("api/v{version:apiVersion}/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController(
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderStatuses(
        [FromQuery] GetOrderStatusesRequest request,
        CancellationToken ct)
    {
        var query = new GetOrderStatusesQuery(request.OnlyActive);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderStatus(Guid id, CancellationToken ct)
    {
        var query = new GetOrderStatusQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrderStatus(
        [FromBody] CreateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderStatusCommand(
            Name: request.Name,
            DisplayName: request.DisplayName,
            Icon: request.Icon,
            Color: request.Color,
            SortOrder: request.SortOrder,
            AllowCancel: request.AllowCancel,
            AllowEdit: request.AllowEdit);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusDefinitionCommand(
            Id: id,
            DisplayName: request.DisplayName,
            Icon: request.Icon,
            Color: request.Color,
            SortOrder: request.SortOrder,
            AllowCancel: request.AllowCancel,
            AllowEdit: request.AllowEdit,
            RowVersion: request.RowVersion);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrderStatus(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderStatusCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateOrderStatus(Guid id, CancellationToken ct)
    {
        var command = new ActivateOrderStatusCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOrderStatus(Guid id, CancellationToken ct)
    {
        var command = new DeactivateOrderStatusCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/set-default")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultOrderStatus(Guid id, CancellationToken ct)
    {
        var command = new SetDefaultOrderStatusCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}