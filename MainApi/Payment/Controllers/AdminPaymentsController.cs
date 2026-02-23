namespace MainApi.Payment.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public AdminPaymentsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentSearchParams searchParams)
    {
        var result = await _mediator.Send(new GetAdminPaymentsQuery(searchParams));
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("{orderId}/refund")]
    public async Task<IActionResult> RefundPayment(int orderId, [FromBody] RefundPaymentRequest request)
    {
        if (!_currentUserService.UserId.HasValue) return Unauthorized();

        var command = new AtomicRefundPaymentCommand(
            orderId,
            _currentUserService.UserId.Value,
            request.Reason,
            request.PartialAmount);

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result.Error);
    }

    [HttpPost("expire-stale")]
    public async Task<IActionResult> ExpireStalePayments([FromQuery] int minutesOld = 30)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutesOld);
        var result = await _mediator.Send(new ExpireStalePaymentsCommand(cutoff));
        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }
}

public record RefundPaymentRequest(string Reason, decimal? PartialAmount = null);