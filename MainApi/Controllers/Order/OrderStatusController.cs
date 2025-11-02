using MainApi.Services.Order;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderStatusController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderStatusController> _logger;

    public OrderStatusController(IOrderService orderService, ILogger<OrderStatusController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTOrderStatus()
    {
        var statuses = await _orderService.GetOrderStatusesAsync();
        return Ok(statuses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        var status = await _orderService.GetOrderStatusByIdAsync(id);
        if (status == null) return NotFound();
        return Ok(status);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderStatus>> PostTOrderStatus(CreateOrderStatusDto statusDto)
    {
        if (statusDto == null || string.IsNullOrWhiteSpace(statusDto.Name))
            return BadRequest("Name is required");

        try
        {
            var orderStatus = await _orderService.CreateOrderStatusAsync(statusDto);
            return CreatedAtAction("GetTOrderStatus", new { id = orderStatus.Id }, orderStatus);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutTOrderStatus(int id, UpdateOrderStatusDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        if (statusDto == null) return BadRequest("Order status data is required");

        try
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, statusDto);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");

        try
        {
            var success = await _orderService.DeleteOrderStatusAsync(id);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}