namespace MainApi.Payment.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentSearchParams searchParams)
    {
        var result = await _mediator.Send(new GetAdminPaymentsQuery(searchParams));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("by-authority/{authority}")]
    public async Task<IActionResult> GetPaymentByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var result = await _mediator.Send(new GetPaymentsByOrderQuery(orderId));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("{orderId}/refund")]
    public async Task<IActionResult> RefundPayment(int orderId, [FromBody] RefundPaymentRequest request)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var command = new AtomicRefundPaymentCommand(
            orderId,
            _currentUserService.UserId.Value,
            request.Reason);

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
    }

    [HttpPost("expire-stale")]
    public async Task<IActionResult> ExpireStalePayments([FromQuery] int minutesOld = 30)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutesOld);
        var result = await _mediator.Send(new ExpireStalePaymentsCommand(cutoff));
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}