using Application.Common.Interfaces.Admin;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService;

    public AdminOrdersController(IAdminOrderService adminOrderService)
    {
        _adminOrderService = adminOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? userId,
        [FromQuery] int? statusId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (orders, totalItems) = await _adminOrderService.GetOrdersAsync(userId, statusId, fromDate, toDate, page, pageSize);

        return Ok(new
        {
            Items = orders,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _adminOrderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound(new { message = "Order not found" });
        }
        return Ok(order);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusByIdDto dto)
    {
        var result = await _adminOrderService.UpdateOrderStatusAsync(id, dto);
        if (!result.Success)
        {
            if (result.Error?.Contains("modified by another user") == true)
            {
                return Conflict(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Order status updated successfully" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var result = await _adminOrderService.UpdateOrderAsync(id, dto);
        if (!result.Success)
        {
            if (result.Error?.Contains("modified by another user") == true)
            {
                return Conflict(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Order updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var result = await _adminOrderService.DeleteOrderAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Order deleted successfully" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var statistics = await _adminOrderService.GetOrderStatisticsAsync(fromDate, toDate);
        return Ok(statistics);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        var order = await _adminOrderService.CreateOrderAsync(dto, idempotencyKey);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, new { orderId = order.Id });
    }
}