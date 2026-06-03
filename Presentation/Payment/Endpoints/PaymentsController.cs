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
        var command = new VerifyPaymentCommand(authority, status);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAuthority(string authority, CancellationToken ct)
    {
        var query = new GetPaymentByAuthorityQuery(authority);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("orders/{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsByOrder(Guid orderId, CancellationToken ct)
    {
        var query = new GetPaymentsByOrderQuery(orderId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{authority}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(string authority, CancellationToken ct)
    {
        var query = new GetPaymentStatusQuery(authority);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PaymentInitiationResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiatePayment(
    [FromBody] InitiatePaymentRequest request,
    CancellationToken ct)
    {
        var command = new InitiatePaymentCommand(
            request.OrderId,
            CurrentUser.UserId,
            request.Gateway,
            CurrentUser.IpAddress);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("webhooks/{gateway}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Webhook(
        [FromBody] WebhookPayloadRequest payload,
        CancellationToken ct)
    {
        var command = new ProcessWebhookCommand(
            payload.Authority,
            payload.Status);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}