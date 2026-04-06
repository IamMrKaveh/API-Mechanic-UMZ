using Presentation.Base.Controllers.v1;

namespace Presentation.Payment.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] PaymentSearchParams searchParams)
    {
        var result = await _mediator.Send(new GetAdminPaymentsQuery(searchParams));
        return ToActionResult(result);
    }

    [HttpGet("by-authority/{authority}")]
    public async Task<IActionResult> GetPaymentByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return ToActionResult(result);
    }

    [HttpGet("by-order/{orderId}")]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var result = await _mediator.Send(new GetPaymentsByOrderQuery(orderId));
        return ToActionResult(result);
    }

    [HttpPost("{orderId}/refund")]
    public async Task<IActionResult> RefundPayment(int orderId, [FromBody] RefundPaymentRequest request)
    {
        var command = new AtomicRefundPaymentCommand(
            orderId,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("expire-stale")]
    public async Task<IActionResult> ExpireStalePayments([FromQuery] int minutesOld = 30)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-minutesOld);
        var result = await _mediator.Send(new ExpireStalePaymentsCommand(cutoff));
        return ToActionResult(result);
    }
}