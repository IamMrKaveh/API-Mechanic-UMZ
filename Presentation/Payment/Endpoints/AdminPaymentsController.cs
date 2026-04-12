using Application.Payment.Features.Commands.AtomicRefundPayment;
using Application.Payment.Features.Queries.GetAdminPayments;
using MapsterMapper;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] AdminPaymentSearchRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAdminPaymentsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken ct)
    {
        var command = new AtomicRefundPaymentCommand(id, CurrentUser.UserId, request.Reason);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}