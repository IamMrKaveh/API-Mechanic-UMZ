namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTOrders(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var isAdmin = User.IsInRole("Admin");
        var currentUserId = GetCurrentUserId();

        if (!userId.HasValue && !isAdmin)
        {
            userId = currentUserId;
        }
        else if (userId.HasValue && userId.Value != currentUserId && !isAdmin)
        {
            return Forbid();
        }

        var (orders, totalItems) = await _orderService.GetOrdersAsync(currentUserId, isAdmin, userId, statusId, fromDate, toDate, page, pageSize);

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
    public async Task<ActionResult<object>> GetOrderById(int id)
    {
        if (id <= 0) return BadRequest("Invalid order ID");

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var order = await _orderService.GetOrderByIdAsync(id, currentUserId, User.IsInRole("Admin"));

        if (order == null) return NotFound("Order not found");

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TOrders>> PostTOrders(CreateOrderDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest("Idempotency-Key header is required.");

        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(orderDto, idempotencyKey);
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            return Conflict("Duplicate request. Order already exists.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order.");
            return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
        }
    }

    [HttpPost("checkout-from-cart")]
    public async Task<ActionResult<TOrders>> CheckoutFromCart([FromBody] CreateOrderFromCartDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized("User not authenticated");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest("Idempotency-Key header is required.");

        try
        {
            var createdOrder = await _orderService.CheckoutFromCartAsync(orderDto, userId.Value, idempotencyKey);
            return Ok(createdOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} with invalid operation.", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} due to concurrency.", userId);
            return Conflict(new { message = "The stock for an item in your cart has changed. Please review your cart and try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout from cart for user {UserId}", userId);
            return StatusCode(500, new { message = "An internal error occurred during checkout." });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutTOrders(int id, UpdateOrderDto orderDto)
    {
        if (id <= 0) return BadRequest("Invalid order ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _orderService.UpdateOrderAsync(id, orderDto);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The order was modified by another user. Please reload and try again." });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, statusDto.OrderStatusId);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency error occurred.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
            return StatusCode(500, "An error occurred while updating the order status");
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetOrderStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var stats = await _orderService.GetOrderStatisticsAsync(fromDate, toDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order statistics.");
            return StatusCode(500, "An error occurred while retrieving statistics");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTOrders(int id)
    {
        if (id <= 0) return BadRequest("Invalid order ID");

        try
        {
            var success = await _orderService.DeleteOrderAsync(id);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the order with ID {OrderId}", id);
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }

    [HttpGet("verify-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment(
    [FromQuery] int orderId,
    [FromQuery] string authority,
    [FromQuery] string status)
    {
        var frontendUrl = _orderService.GetFrontendUrl();

        if (status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            var isVerified = await _orderService.VerifyPaymentAsync(orderId, authority);
            if (isVerified)
            {
                return Redirect($"{frontendUrl}/payment/success?orderId={orderId}");
            }
        }

        return Redirect($"{frontendUrl}/payment/failure?orderId={orderId}");
    }
}