namespace MainApi.Order.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminOrdersController(ICurrentUserService currentUserService, IMediator mediator)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var query = new GetAdminOrderByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return Ok(result);
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
    public async Task<IActionResult> DeleteOrder(int orderId, int userId)
    {
        var command = new DeleteOrderCommand(orderId, userId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = new GetOrderStatisticsQuery(fromDate, toDate);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] AdminCreateOrderDto dto, int userId)
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(dto, idempotencyKey, userId);
        var result = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data }, new { orderId = result.Data });
    }

    [HttpPatch("{id}/ship")]
    public async Task<IActionResult> MarkAsShipped(int id, [FromBody] MarkAsShippedRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new MarkOrderAsShippedCommand
        {
            OrderId = id,
            RowVersion = request.RowVersion,
            UpdatedByUserId = CurrentUser.UserId.Value
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("expire")]
    public async Task<IActionResult> ExpireOrders()
    {
        var result = await _mediator.Send(new ExpireOrdersCommand());
        return Ok(result);
    }
}

public record MarkAsShippedRequest(string RowVersion);