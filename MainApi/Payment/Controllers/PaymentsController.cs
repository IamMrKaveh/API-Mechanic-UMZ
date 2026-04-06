using Application.Payment.Features.Commands.InitiatePayment;
using Application.Payment.Features.Commands.ProcessWebhook;
using Application.Payment.Features.Commands.VerifyPayment;
using Application.Payment.Features.Queries.GetPaymentByAuthority;
using Application.Payment.Features.Queries.GetPaymentsByOrder;
using Application.Payment.Features.Queries.GetPaymentStatus;
using Application.Payment.Features.Shared;
using Presentation.Base.Controllers.v1;

namespace Presentation.Payment.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("initiate")]
    [Authorize]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationDto dto)
    {
        var command = new InitiatePaymentCommand(dto, CurrentUser.UserId, CurrentUser.IpAddress ?? "Unknown");
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        var command = new VerifyPaymentCommand(authority, status);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return ToActionResult(result);
    }

    [HttpGet("by-order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var result = await _mediator.Send(new GetPaymentsByOrderQuery(orderId));
        return ToActionResult(result);
    }

    [HttpGet("status/{authority}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(string authority)
    {
        var result = await _mediator.Send(new GetPaymentStatusQuery(authority));
        return ToActionResult(result);
    }

    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(string gateway, [FromBody] WebhookPayload payload)
    {
        var command = new ProcessWebhookCommand(gateway, payload.Authority, payload.Status, payload.RefId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}