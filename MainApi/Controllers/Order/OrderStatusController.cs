using MainApi.Services.Order;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderStatusController : BaseApiController
{
    private readonly IOrderStatusService _orderStatusService;
    private readonly ILogger<OrderStatusController> _logger;

    public OrderStatusController(IOrderStatusService orderStatusService, ILogger<OrderStatusController> logger)
    {
        _orderStatusService = orderStatusService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TOrderStatus>>> GetTOrderStatus()
    {
        var statuses = await _orderStatusService.GetOrderStatusesAsync();
        return Ok(statuses);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<TOrderStatus>> GetTOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        var status = await _orderStatusService.GetOrderStatusByIdAsync(id);
        if (status == null) return NotFound();
        return Ok(status);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderStatus>> PostTOrderStatus(CreateOrderStatusDto statusDto)
    {
        if (statusDto == null) return BadRequest("Status data is required");

        try
        {
            var orderStatus = await _orderStatusService.CreateOrderStatusAsync(statusDto);
            return CreatedAtAction(nameof(GetTOrderStatus), new { id = orderStatus.Id }, orderStatus);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order status");
            return StatusCode(500, "An unexpected error occurred.");
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
            // The service method for this in the original code was on OrderService, 
            // but the logic only updated the status name/icon, not the order's status.
            // So moving it to OrderStatusService makes more sense.
            var success = await _orderStatusService.UpdateOrderStatusAsync(id, statusDto);
            return success ? NoContent() : NotFound();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The status was modified by another user. Please reload.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");

        try
        {
            var success = await _orderStatusService.DeleteOrderStatusAsync(id);
            return success ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order status {Id}", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}