namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserOrdersQuery(CurrentUser.UserId.Value, status, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetOrderDetailsQuery(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("checkout-from-cart")]
    public async Task<IActionResult> CheckoutFromCart(
        [FromBody] CreateOrderFromCartDto dto,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
            return BadRequest(new { message = "Idempotency-Key header is required" });

        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new CheckoutFromCartCommand
        {
            UserId = CurrentUser.UserId.Value,
            UserAddressId = dto.UserAddressId,
            NewAddress = dto.NewAddress,
            SaveNewAddress = dto.SaveNewAddress,
            ShippingId = dto.ShippingId,
            DiscountCode = dto.DiscountCode,
            ExpectedItems = dto.ExpectedItems,
            CallbackUrl = dto.CallbackUrl,
            IdempotencyKey = idempotencyKey
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("verify-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment(
        [FromQuery] string authority,
        [FromQuery] string status)
    {
        var command = new VerifyPaymentCommand(authority, status);
        var result = await _mediator.Send(command);

        if (result.IsSucceed && !string.IsNullOrEmpty(result.Data?.RedirectUrl))
        {
            return Redirect(result.Data.RedirectUrl);
        }

        return ToActionResult(result);
    }

    [HttpPost("validate-discount")]
    public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new ValidateDiscountQuery(request.Code, request.OrderTotal, CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new CancelOrderCommand
        {
            OrderId = id,
            UserId = CurrentUser.UserId.Value,
            IsAdmin = false,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}

public record CancelOrderRequest(string Reason);