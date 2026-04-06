using Application.Features.Orders.Commands.DeleteOrder;
using Application.Order.Features.Commands.CreateOrder;
using Application.Order.Features.Commands.ExpireOrders;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Application.Order.Features.Commands.UpdateOrder;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Application.Order.Features.Shared;
using Presentation.Base.Controllers.v1;

namespace Presentation.Order.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? userId,
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
    public async Task<IActionResult> GetOrderById(int id)
    {
        var query = new GetAdminOrderByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusByIdDto dto)
    {
        var command = new UpdateOrderStatusCommand(id, dto.OrderStatusId, dto.RowVersion, dto.UpdatedByUserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var command = new UpdateOrderCommand(id, dto);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var command = new DeleteOrderCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetOrderStatisticsQuery(fromDate, toDate);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] AdminCreateOrderDto dto)
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(dto, idempotencyKey, CurrentUser.UserId);
        var result = await _mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{id}/ship")]
    public async Task<IActionResult> MarkAsShipped(int id, [FromBody] MarkAsShippedRequest request)
    {
        var command = new MarkOrderAsShippedCommand
        {
            OrderId = id,
            RowVersion = request.RowVersion,
            UpdatedByUserId = CurrentUser.UserId
        };

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