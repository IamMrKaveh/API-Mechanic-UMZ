namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, IDiscountService discountService, ICurrentUserService currentUserService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _discountService = discountService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMyOrders(
        [FromQuery] int? statusId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue) return Forbid();

        var (orders, totalItems) = await _orderService.GetOrdersAsync(currentUserId, false, currentUserId, statusId, fromDate, toDate, page, pageSize);

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
            var (createdOrder, paymentUrl, error) = await _orderService.CheckoutFromCartAsync(orderDto, userId.Value, idempotencyKey);

            if (paymentUrl != null)
            {
                return Ok(new { PaymentUrl = paymentUrl, OrderId = createdOrder.Id });
            }

            _logger.LogError("Payment URL is null for order {OrderId}. Reason: {Reason}", createdOrder.Id, error);
            return StatusCode(500, new { message = error ?? "Failed to generate payment link." });
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

    [HttpGet("verify-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status, [FromQuery] int orderId)
    {
        var (isVerified, redirectUrl) = await _orderService.VerifyAndProcessPaymentAsync(orderId, authority, status);

        return Redirect(redirectUrl);
    }
}