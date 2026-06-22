using Application.Payment.Features.Commands.InitiatePayment;
using Application.Payment.Features.Commands.ProcessWebhook;
using Application.Payment.Features.Commands.VerifyPayment;
using Application.Payment.Features.Queries.GetPaymentByAuthority;
using Application.Payment.Features.Queries.GetPaymentsByOrder;
using Application.Payment.Features.Queries.GetPaymentStatus;
using Application.Payment.Features.Shared;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[Route("api/v{version:apiVersion}/payments")]
[ApiController]
public class PaymentsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("verify")]
    [ProducesResponseType(typeof(ApiResponse<PaymentVerificationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyPayment(
        [FromQuery] string authority,
        [FromQuery] string status,
        CancellationToken ct)
    {
        return await Send(new VerifyPaymentCommand(authority, status), ct);
    }

    [HttpGet("{authority}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAuthority(string authority, CancellationToken ct)
    {
        return await Send(new GetPaymentByAuthorityQuery(authority), ct);
    }

    [HttpGet("orders/{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsByOrder(Guid orderId, CancellationToken ct)
    {
        return await Send(new GetPaymentsByOrderQuery(orderId), ct);
    }

    [HttpGet("{authority}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(string authority, CancellationToken ct)
    {
        return await Send(new GetPaymentStatusQuery(authority), ct);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentInitiationResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new InitiatePaymentCommand(request.OrderId, request.Gateway), ct);
        return ToCreatedActionResult(result);
    }

    [HttpPost("webhooks/{gateway}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Webhook(
        [FromBody] WebhookPayloadRequest payload,
        CancellationToken ct)
    {
        return await Send(new ProcessWebhookCommand(payload.Authority, payload.Status), ct);
    }
}