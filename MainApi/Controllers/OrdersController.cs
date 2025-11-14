namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly IZarinpalService _zarinpalService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrdersController> _logger;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;
    private readonly IOptions<ZarinpalSettingsDto> _zarinpalSettings;

    public OrdersController(IOrderService orderService, IDiscountService discountService, IZarinpalService zarinpalService, ICurrentUserService currentUserService, ILogger<OrdersController> logger, IOptions<FrontendUrlsDto> frontendUrls, IOptions<ZarinpalSettingsDto> zarinpalSettings)
    {
        _orderService = orderService;
        _discountService = discountService;
        _zarinpalService = zarinpalService;
        _currentUserService = currentUserService;
        _logger = logger;
        _frontendUrls = frontendUrls;
        _zarinpalSettings = zarinpalSettings;
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

        var currentUserId = _currentUserService.UserId;

        if (!userId.HasValue && !_currentUserService.IsAdmin)
        {
            userId = currentUserId;
        }
        else if (userId.HasValue && userId.Value != currentUserId && !_currentUserService.IsAdmin)
        {
            return Forbid();
        }

        var (orders, totalItems) = await _orderService.GetOrdersAsync(currentUserId, _currentUserService.IsAdmin, userId, statusId, fromDate, toDate, page, pageSize);

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

        var currentUserId = _currentUserService.UserId;
        if (currentUserId == null) return Unauthorized();

        var order = await _orderService.GetOrderByIdAsync(id, currentUserId, _currentUserService.IsAdmin);

        if (order == null) return NotFound("Order not found");

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Domain.Order.Order>> PostOrder([FromBody] CreateOrderDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized("User not authenticated");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest("Idempotency-Key header is required.");

        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(orderDto, idempotencyKey);
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order.");
            return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
        }
    }

    [HttpPost("checkout-from-cart")]
    public async Task<ActionResult<object>> CheckoutFromCart([FromBody] CreateOrderFromCartDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized("User not authenticated");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest("Idempotency-Key header is required.");

        try
        {
            var createdOrder = await _orderService.CheckoutFromCartAsync(orderDto, userId.Value, idempotencyKey);

            var callbackUrl = $"{_frontendUrls.Value.BaseUrl}/payment-verify?orderId={createdOrder.Id}";
            var paymentResponse = await _zarinpalService.CreatePaymentRequestAsync(_zarinpalSettings.Value, createdOrder.FinalAmount, $"پرداخت سفارش شماره {createdOrder.Id}", callbackUrl, createdOrder.User?.PhoneNumber);

            if (paymentResponse?.Data?.Code == 100 && !string.IsNullOrEmpty(paymentResponse.Data.Authority))
            {
                var gatewayUrl = _zarinpalService.GetPaymentGatewayUrl(_zarinpalSettings.Value.IsSandbox, paymentResponse.Data.Authority);
                return Ok(new { PaymentUrl = gatewayUrl });
            }

            var message = paymentResponse?.Data?.Message ?? "Failed to generate payment link.";
            _logger.LogError("Zarinpal payment URL is null for order {OrderId}. Reason: {Reason}", createdOrder.Id, message);
            return StatusCode(500, new { message });
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} with invalid argument.", userId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout from cart for user {UserId}", userId);
            return StatusCode(500, new { message = "An internal error occurred during checkout." });
        }
    }

    [HttpPost("validate-discount")]
    public async Task<IActionResult> ValidateDiscount([FromBody] ApplyDiscountDto dto)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var (discount, error) = await _discountService.ValidateAndGetDiscountAsync(dto.Code, userId.Value, dto.OrderTotal);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(discount);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutOrder(int id, UpdateOrderDto orderDto)
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
            return BadRequest(new { Message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The order was modified by another user. Please reload and try again." });
        }
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusByIdDto statusDto)
    {
        if (id <= 0) return BadRequest("Invalid order ID");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var success = await _orderService.UpdateOrderStatusAsync(id, statusDto);
            return success ? NoContent() : NotFound("Order not found or status ID is invalid.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
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
    public async Task<IActionResult> DeleteOrder(int id)
    {
        if (id <= 0) return BadRequest("Invalid order ID");

        try
        {
            var success = await _orderService.DeleteOrderAsync(id);
            return success ? NoContent() : NotFound("Order not found");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the order with ID {OrderId}", id);
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }

    [HttpGet("verify-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status, [FromQuery] int orderId)
    {
        var (isVerified, redirectUrl) = await _orderService.VerifyAndProcessPaymentAsync(orderId, authority, status);

        return Redirect(redirectUrl);
    }
}