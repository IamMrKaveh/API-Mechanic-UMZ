using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : BaseApiController
{
    private readonly IOrderItemService _orderItemService;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(IOrderItemService orderItemService, ILogger<OrderItemsController> logger)
    {
        _orderItemService = orderItemService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetOrderItems(
        [FromQuery] int? orderId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var (items, total) = await _orderItemService.GetOrderItemsAsync(GetCurrentUserId(), User.IsInRole("Admin"), orderId, page, pageSize);
            return Ok(new
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order items for order {OrderId}", orderId);
            return StatusCode(500, "An error occurred while retrieving order items.");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");

        var item = await _orderItemService.GetOrderItemByIdAsync(id, GetCurrentUserId(), User.IsInRole("Admin"));
        if (item == null) return NotFound("Order item not found or you do not have permission to view it.");

        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrderItems>> CreateOrderItem(CreateOrderItemDto itemDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var orderItem = await _orderItemService.CreateOrderItemAsync(itemDto);
            var result = await _orderItemService.GetOrderItemByIdAsync(orderItem.Id, GetCurrentUserId(), true);
            return CreatedAtAction(nameof(GetOrderItem), new { id = orderItem.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order item.");
            return StatusCode(500, "An unexpected error occurred while creating the order item.");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderItem(int id, UpdateOrderItemDto itemDto)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var success = await _orderItemService.UpdateOrderItemAsync(id, itemDto, userId.Value);
            return success ? NoContent() : NotFound("Order item not found.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Data has been modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order item {Id}", id);
            return StatusCode(500, "An error occurred while updating the order item.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var success = await _orderItemService.DeleteOrderItemAsync(id, userId.Value);
            return success ? NoContent() : NotFound("Order item not found.");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order item {Id}", id);
            return StatusCode(500, "An error occurred while deleting the order item.");
        }
    }
}