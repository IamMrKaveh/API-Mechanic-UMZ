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
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        // Assuming UserId extracted from Claims via a helper or base class, typically:
        var userId = int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        var command = new InitiatePaymentCommand(dto, userId, ip);
        var result = await _mediator.Send(command);

        return result.IsSucceed ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        // This endpoint typically handles the callback from the bank
        var command = new VerifyPaymentCommand(authority, status);
        var result = await _mediator.Send(command);

        if (result.IsSucceed && result.Data?.RedirectUrl != null)
        {
            return Redirect(result.Data.RedirectUrl);
        }

        // Fallback if redirect fails
        return Ok(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return result.IsSucceed ? Ok(result.Data) : NotFound(result.Error);
    }

    // Webhook example (if supported by gateway)
    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(string gateway, [FromBody] WebhookPayload payload)
    {
        // Payload mapping would depend on gateway
        var command = new ProcessWebhookCommand(gateway, payload.Authority, payload.Status, payload.RefId);
        await _mediator.Send(command);
        return Ok();
    }

    // DTO for webhook binding
    public class WebhookPayload
    {
        public string Authority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long? RefId { get; set; }
    }
}