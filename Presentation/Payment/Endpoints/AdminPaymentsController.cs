using Application.Payment.Features.Commands.AtomicRefundPayment;
using Application.Payment.Features.Queries.GetAdminPayments;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] AdminPaymentSearchRequest request)
    {
        var query = new GetAdminPaymentsQuery(
            request.OrderId,
            request.UserId,
            request.Status,
            request.Gateway,
            request.FromDate,
            request.ToDate);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("{id}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
    {
        var command = new AtomicRefundPaymentCommand(id, CurrentUser.UserId, request.Reason);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}