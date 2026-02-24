namespace MainApi.Payment.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initiate")]
    [Authorize]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userId = int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        var command = new InitiatePaymentCommand(dto, userId, ip);
        var result = await _mediator.Send(command);

        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        var command = new VerifyPaymentCommand(authority, status);
        var result = await _mediator.Send(command);

        if (result.IsSucceed && result.Data?.RedirectUrl != null)
        {
            return Redirect(result.Data.RedirectUrl);
        }

        return Ok(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return result.IsSucceed ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("by-order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var result = await _mediator.Send(new GetPaymentsByOrderQuery(orderId));
        return result.IsSucceed ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpGet("status/{authority}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(string authority)
    {
        var result = await _mediator.Send(new GetPaymentStatusQuery(authority));
        return result.IsSucceed ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(string gateway, [FromBody] WebhookPayload payload)
    {
        var command = new ProcessWebhookCommand(gateway, payload.Authority, payload.Status, payload.RefId);
        await _mediator.Send(command);
        return Ok();
    }

    public class WebhookPayload
    {
        public string Authority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long? RefId { get; set; }
    }
}