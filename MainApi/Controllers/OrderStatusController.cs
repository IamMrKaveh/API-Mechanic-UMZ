using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderStatusController : ControllerBase
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
    public async Task<ActionResult<IEnumerable<Domain.Order.OrderStatus>>> GetOrderStatuses()
    {
        var statuses = await _orderStatusService.GetOrderStatusesAsync();
        return Ok(statuses);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Domain.Order.OrderStatus>> GetOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        var status = await _orderStatusService.GetOrderStatusByIdAsync(id);
        if (status == null) return NotFound();
        return Ok(status);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Domain.Order.OrderStatus>> PostOrderStatus(CreateOrderStatusDto statusDto)
    {
        if (statusDto == null) return BadRequest("Status data is required");

        try
        {
            var orderStatus = await _orderStatusService.CreateOrderStatusAsync(statusDto);
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutOrderStatus(int id, UpdateOrderStatusDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        if (statusDto == null) return BadRequest("Order status data is required");

        try
        {
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
    public async Task<IActionResult> DeleteOrderStatus(int id)
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