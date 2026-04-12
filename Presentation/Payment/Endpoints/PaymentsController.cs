using Application.Payment.Features.Commands.InitiatePayment;
using Application.Payment.Features.Commands.ProcessWebhook;
using Application.Payment.Features.Commands.VerifyPayment;
using Application.Payment.Features.Queries.GetPaymentByAuthority;
using Application.Payment.Features.Queries.GetPaymentsByOrder;
using Application.Payment.Features.Queries.GetPaymentStatus;
using MapsterMapper;
using Presentation.Payment.Requests;

namespace Presentation.Payment.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpPost("initiate")]
    [Authorize]
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

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyPayment(
        [FromQuery] string authority,
        [FromQuery] string status,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new VerifyPaymentCommand(authority, status), ct);
        return ToActionResult(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthority(string authority, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPaymentByAuthorityQuery(authority), ct);
        return ToActionResult(result);
    }

    [HttpGet("by-order/{orderId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrder(Guid orderId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPaymentsByOrderQuery(orderId), ct);
        return ToActionResult(result);
    }

    [HttpGet("status/{authority}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(string authority, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPaymentStatusQuery(authority), ct);
        return ToActionResult(result);
    }

    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(
        [FromBody] WebhookPayload payload,
        CancellationToken ct)
    {
        var command = new ProcessWebhookCommand(
            payload.Authority,
            payload.Status);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}