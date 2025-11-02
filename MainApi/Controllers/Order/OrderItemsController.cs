using MainApi.Services.Order;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(IOrderService orderService, ILogger<OrderItemsController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrderItems([FromQuery] int? orderId = null)
    {
        try
        {
            var items = await _orderService.GetOrderItemsAsync(GetCurrentUserId(), User.IsInRole("Admin"), orderId);
            return Ok(items);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");

        var item = await _orderService.GetOrderItemByIdAsync(id, GetCurrentUserId(), User.IsInRole("Admin"));
        if (item == null) return NotFound("Order item not found");

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderItems>> CreateOrderItem(CreateOrderItemDto itemDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var orderItem = await _orderService.CreateOrderItemAsync(itemDto);
            return CreatedAtAction("GetOrderItem", new { id = orderItem.Id }, orderItem);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The item's stock has changed. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order item.");
            return StatusCode(500, "Error creating order item");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderItem(int id, UpdateOrderItemDto itemDto)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _orderService.UpdateOrderItemAsync(id, itemDto);
            return success ? NoContent() : NotFound("Order item not found");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Data has been modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order item {Id}", id);
            return StatusCode(500, "Error updating order item");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");

        try
        {
            var success = await _orderService.DeleteOrderItemAsync(id);
            return success ? NoContent() : NotFound("Order item not found");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The item's stock or order has changed. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order item {Id}", id);
            return StatusCode(500, "Error deleting order item");
        }
    }
}