using Application.Common.Interfaces.Discount;
using Application.Common.Interfaces.Order;
using Application.Common.Interfaces.User;
using Application.DTOs.Order;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDiscountService _discountService;

    public OrdersController(
        IOrderService orderService,
        ICurrentUserService currentUserService,
        IDiscountService discountService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int? statusId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = _currentUserService.UserId;
        var (orders, totalItems) = await _orderService.GetOrdersAsync(userId, false, userId, statusId, fromDate, toDate, page, pageSize);
        return Ok(new { items = orders, totalCount = totalItems, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = User.IsInRole("Admin");
        var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin);
        if (order == null) return NotFound(new { message = "سفارش مورد نظر یافت نشد" });
        return Ok(order);
    }

    [HttpPost("checkout-from-cart")]
    public async Task<IActionResult> CheckoutFromCart([FromBody] CreateOrderFromCartDto dto, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey)) return BadRequest(new { message = "Idempotency-Key header is required" });
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var result = await _orderService.CheckoutFromCartAsync(dto, userId.Value, idempotencyKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("verify-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status, [FromQuery] int orderId)
    {
        var result = await _orderService.VerifyAndProcessPaymentAsync(orderId, authority, status);
        return Ok(result);
    }

    [HttpPost("validate-discount")]
    public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountDto dto)
    {
        var userId = _currentUserService.UserId ?? 0;
        var result = await _discountService.ValidateAndApplyDiscountAsync(dto.Code, dto.OrderTotal, userId);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var result = await _orderService.CancelOrderAsync(id, userId.Value);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return Ok(new { message = "سفارش با موفقیت لغو شد" });
    }
}