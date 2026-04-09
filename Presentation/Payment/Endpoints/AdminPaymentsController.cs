using Application.Payment.Features.Commands.AtomicRefundPayment;
using Application.Payment.Features.Queries.GetAdminPayments;
using Application.Payment.Features.Shared;
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
        var searchParams = new PaymentSearchParams
        {
            Page = request.Page,
            PageSize = request.PageSize,
            Status = request.Status,
            UserId = request.UserId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Gateway = request.Gateway
        };

        var query = new GetAdminPaymentsQuery(searchParams);
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