using Application.Order.Features.Commands.DeleteOrder;
using Application.Order.Features.Commands.ExpireOrders;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] bool? isPaid,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminOrdersQuery(userId, status, fromDate, toDate, isPaid, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var result = await _mediator.Send(new GetAdminOrderByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusByIdRequest request)
    {
        var command = new UpdateOrderStatusCommand(
            id,
            request.OrderStatusId,
            request.RowVersion,
            CurrentUser.UserId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var command = new DeleteOrderCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _mediator.Send(new GetOrderStatisticsQuery(fromDate, toDate));
        return ToActionResult(result);
    }

    [HttpPatch("{id}/ship")]
    public async Task<IActionResult> MarkAsShipped(
        Guid orderId)
    {
        var command = new MarkOrderAsShippedCommand(orderId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("expire")]
    public async Task<IActionResult> ExpireOrders()
    {
        var result = await _mediator.Send(new ExpireOrdersCommand());
        return ToActionResult(result);
    }
}