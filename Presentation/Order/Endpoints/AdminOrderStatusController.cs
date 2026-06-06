using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderStatuses(CancellationToken ct)
    {
        var query = new GetOrderStatusesQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
            request.AllowCancel,
            request.AllowEdit);

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
}