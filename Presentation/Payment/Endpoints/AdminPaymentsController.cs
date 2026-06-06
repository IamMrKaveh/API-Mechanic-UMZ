using Application.Payment.Features.Commands.AtomicRefundPayment;
using Application.Payment.Features.Queries.GetAdminPayments;
using Application.Payment.Features.Shared;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController(
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<PaymentTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] AdminPaymentSearchRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAdminPaymentsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/refunds")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken ct)
    {
        var command = new AtomicRefundPaymentCommand(id, request.Reason);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}