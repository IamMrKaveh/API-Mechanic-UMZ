namespace MainApi.Controllers.Admin;

[Route("api/admin/orders")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminOrdersController> _logger;

    public AdminOrdersController(IAdminOrderService adminOrderService, ICurrentUserService currentUserService, ILogger<AdminOrdersController> logger)
    {
        _adminOrderService = adminOrderService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetOrders(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

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

    [HttpPost]
    public async Task<ActionResult<Domain.Order.Order>> PostOrder([FromBody] CreateOrderDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized("User not authenticated");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest("Idempotency-Key header is required.");

        try
        {
            var createdOrder = await _adminOrderService.CreateOrderAsync(orderDto, idempotencyKey);

            var orderResult = await _adminOrderService.GetOrderByIdAsync(createdOrder.Id);

            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, orderResult);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            _logger.LogWarning("Idempotency key violation for key: {IdempotencyKey}", idempotencyKey);
            return Conflict("Duplicate request. Order already exists with this idempotency key.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetOrderById(int id)
    {
        if (id <= 0) return BadRequest("Invalid order ID");

        var order = await _adminOrderService.GetOrderByIdAsync(id);

        if (order == null) return NotFound("Order not found");

        return Ok(order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrder(int id, UpdateOrderDto orderDto)
    {
        if (id <= 0) return BadRequest("Invalid order ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _adminOrderService.UpdateOrderAsync(id, orderDto);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The order was modified by another user. Please reload and try again." });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusByIdDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _adminOrderService.UpdateOrderStatusAsync(id, statusDto);
            return success ? NoContent() : NotFound("Order not found or status ID is invalid.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("statistics")]
    [OutputCache(PolicyName = "LongCache")]
    public async Task<ActionResult<object>> GetOrderStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var stats = await _adminOrderService.GetOrderStatisticsAsync(fromDate, toDate);
        return Ok(stats);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        if (id <= 0) return BadRequest("Invalid order ID");

        try
        {
            var success = await _adminOrderService.DeleteOrderAsync(id);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { ex.Message });
        }
    }
}