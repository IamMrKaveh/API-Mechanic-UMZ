namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : ControllerBase
{
    private readonly IOrderItemService _orderItemService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(IOrderItemService orderItemService, ICurrentUserService currentUserService, ILogger<OrderItemsController> logger)
    {
        _orderItemService = orderItemService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Domain.Order.OrderItem>> CreateOrderItem(CreateOrderItemDto itemDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _orderItemService.CreateOrderItemAsync(itemDto, userId.Value);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { Message = result.Error });
            }

            var itemResult = await _orderItemService.GetOrderItemByIdAsync(result.Data.Id);

            return CreatedAtAction(nameof(GetOrderItem), new { id = result.Data.Id }, itemResult.Data);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");

        var result = await _orderItemService.GetOrderItemByIdAsync(id);
        if (!result.Success || result.Data == null)
        {
            return NotFound(new { Message = result.Error });
        }

        // Add authorization check if necessary for admin/user roles
        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderItem(int id, UpdateOrderItemDto itemDto)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _orderItemService.UpdateOrderItemAsync(id, itemDto, userId.Value);
            if (result.Success) return NoContent();

            return result.StatusCode switch
            {
                404 => NotFound(new { Message = result.Error }),
                409 => Conflict(new { Message = result.Error }),
                _ => BadRequest(new { Message = result.Error })
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Data has been modified by another user. Please refresh and try again.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        if (id <= 0) return BadRequest("Invalid order item ID");
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _orderItemService.DeleteOrderItemAsync(id, userId.Value);
        if (result.Success) return NoContent();
        return NotFound(new { Message = result.Error });
    }
}