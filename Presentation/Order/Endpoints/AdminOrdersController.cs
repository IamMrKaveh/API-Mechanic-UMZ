using Application.Order.Features.Commands.DeleteOrder;
using Application.Order.Features.Commands.ExpireOrders;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using MapsterMapper;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] GetAdminOrdersRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAdminOrdersQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAdminOrderByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusByIdRequest request,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusCommand(
            id,
            request.OrderStatusId,
            request.RowVersion,
            CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderCommand(id, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] GetOrderStatisticsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetOrderStatisticsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/ship")]
    public async Task<IActionResult> MarkAsShipped(Guid id, CancellationToken ct)
    {
        var command = new MarkOrderAsShippedCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("expire")]
    public async Task<IActionResult> ExpireOrders(CancellationToken ct)
    {
        var result = await Mediator.Send(new ExpireOrdersCommand(), ct);
        return ToActionResult(result);
    }
}