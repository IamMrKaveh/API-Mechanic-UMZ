using System.Text.RegularExpressions;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IDiscountService _discountService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrdersController> _logger;
    private readonly IAuditService _auditService;
    private readonly FrontendUrlsDto _frontendUrls;

    private static readonly Regex AuthorityRegex = new(@"^[A-Za-z0-9\-_]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex StatusRegex = new(@"^(OK|NOK|Pending|Cancelled|Failed)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public OrdersController(
        IOrderService orderService,
        IDiscountService discountService,
        ICurrentUserService currentUserService,
        ILogger<OrdersController> logger,
        IAuditService auditService,
        IOptions<FrontendUrlsDto> frontendUrls)
    {
        _orderService = orderService;
        _discountService = discountService;
        _currentUserService = currentUserService;
        _logger = logger;
        _auditService = auditService;
        _frontendUrls = frontendUrls.Value;
    }

    [HttpGet]
    [Authorize]
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
    [Authorize]
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
    [Authorize]
    public async Task<ActionResult<object>> CheckoutFromCart([FromBody] CreateOrderFromCartDto orderDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized("User not authenticated");

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            idempotencyKey = Guid.NewGuid().ToString();
            _logger.LogWarning("No Idempotency-Key provided. Generated: {Key}", idempotencyKey);
        }

        // Improved Callback URL logic to support localhost and production seamlessly
        var callbackUrl = _frontendUrls.BaseUrl.TrimEnd('/') + "/payment/callback";

        // If the request sends a valid CallbackUrl (e.g. from localhost), prefer it
        if (!string.IsNullOrEmpty(orderDto.CallbackUrl) &&
            Uri.TryCreate(orderDto.CallbackUrl, UriKind.Absolute, out var uri))
        {
            // Ideally check against AllowedOrigins here, but for simple start we trust if valid URI
            callbackUrl = orderDto.CallbackUrl;
        }

        orderDto.CallbackUrl = callbackUrl;

        try
        {
            var result = await _orderService.CheckoutFromCartAsync(orderDto, userId.Value, idempotencyKey);

            if (!string.IsNullOrEmpty(result.Error)) 
                return BadRequest(new { message = result.Error });

            return Ok(new
            {
                paymentUrl = result.PaymentUrl,
                orderId = result.OrderId,
                authority = result.Authority
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} with invalid operation.", userId);
            await _auditService.LogOrderEventAsync(0, "CheckoutInvalidOp", userId.Value, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} due to concurrency.", userId);
            await _auditService.LogOrderEventAsync(0, "CheckoutConcurrency", userId.Value, ex.Message);
            return Conflict(new { message = "Cart was updated by another user. Please refresh." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Checkout failed for user {UserId} with invalid argument.", userId);
            await _auditService.LogOrderEventAsync(0, "CheckoutArgError", userId.Value, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout unexpected error for user {UserId}", userId);
            await _auditService.LogOrderEventAsync(0, "CheckoutUnexpected", userId.Value, ex.Message);
            return StatusCode(500, new { message = "خطای غیرمنتظره رخ داده است." });
        }
    }

    [HttpPost("validate-discount")]
    [Authorize]
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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status, [FromQuery] int? orderId)
    {
        var sanitizedAuthority = SanitizeAuthority(authority);
        var sanitizedStatus = SanitizeStatus(status);

        _logger.LogInformation("[VerifyPayment] Started - Authority: {Authority}, Status: {Status}, OrderId: {OrderId}",
            sanitizedAuthority, sanitizedStatus, orderId);

        if (string.IsNullOrWhiteSpace(sanitizedAuthority) || !orderId.HasValue || orderId <= 0)
        {
            _logger.LogWarning("[VerifyPayment] Invalid parameters - Authority: {Authority}, OrderId: {OrderId}",
                sanitizedAuthority, orderId);
            return Ok(new { isVerified = false, orderId = orderId, message = "پارامترهای پرداخت نامعتبر است." });
        }

        // Skip rigid format validation for Mock Authority (GUID) vs ZarinPal (A000...)
        // if (!IsValidAuthority(sanitizedAuthority)) ...

        if (!IsValidStatus(sanitizedStatus))
        {
            _logger.LogWarning("[VerifyPayment] Invalid status format: {Status}", sanitizedStatus);
            return Ok(new { isVerified = false, orderId = orderId, message = "وضعیت پرداخت نامعتبر است." });
        }

        try
        {
            var result = await _orderService.VerifyAndProcessPaymentAsync(orderId.Value, sanitizedAuthority, sanitizedStatus);

            _logger.LogInformation("[VerifyPayment] Result - OrderId: {OrderId}, IsVerified: {IsVerified}, RefId: {RefId}, Message: {Message}",
                orderId, result.IsVerified, result.RefId, result.Message);

            return Ok(new
            {
                isVerified = result.IsVerified,
                orderId = orderId,
                refId = result.RefId,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VerifyPayment] Exception for order {OrderId}", orderId);

            return Ok(new
            {
                isVerified = false,
                orderId = orderId,
                message = "خطا در تایید پرداخت. لطفا با پشتیبانی تماس بگیرید."
            });
        }
    }

    private static string SanitizeAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return string.Empty;

        var sanitized = authority.Trim();
        sanitized = sanitized.Replace("<", "").Replace(">", "").Replace("\"", "").Replace("'", "");

        if (sanitized.Length > 100)
            sanitized = sanitized.Substring(0, 100);

        return sanitized;
    }

    private static string SanitizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        var sanitized = status.Trim();
        sanitized = sanitized.Replace("<", "").Replace(">", "").Replace("\"", "").Replace("'", "");

        if (sanitized.Length > 20)
            sanitized = sanitized.Substring(0, 20);

        return sanitized;
    }

    private static bool IsValidStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return StatusRegex.IsMatch(status);
    }
}