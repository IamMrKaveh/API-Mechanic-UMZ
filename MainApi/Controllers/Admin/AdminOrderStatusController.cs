using Application.Common.Interfaces.Admin;

namespace MainApi.Controllers.Admin;

[Route("api/admin/order-statuses")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrderStatusController : ControllerBase
{
    private readonly IAdminOrderStatusService _adminOrderStatusService;
    private readonly ILogger<AdminOrderStatusController> _logger;

    public AdminOrderStatusController(IAdminOrderStatusService adminOrderStatusService, ILogger<AdminOrderStatusController> logger)
    {
        _adminOrderStatusService = adminOrderStatusService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Domain.Order.OrderStatus>> PostOrderStatus(CreateOrderStatusDto statusDto)
    {
        if (statusDto == null) return BadRequest("Status data is required");

        try
        {
            var orderStatus = await _adminOrderStatusService.CreateOrderStatusAsync(statusDto);
            return CreatedAtAction(nameof(GetOrderStatus), new { id = orderStatus.Id }, orderStatus);
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

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Domain.Order.OrderStatus>> GetOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        var status = await _adminOrderStatusService.GetOrderStatusByIdAsync(id);
        if (status == null) return NotFound();
        return Ok(status);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrderStatus(int id, UpdateOrderStatusDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        if (statusDto == null) return BadRequest("Order status data is required");

        try
        {
            var success = await _adminOrderStatusService.UpdateOrderStatusAsync(id, statusDto);
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
    public async Task<IActionResult> DeleteOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");

        try
        {
            var success = await _adminOrderStatusService.DeleteOrderStatusAsync(id);
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