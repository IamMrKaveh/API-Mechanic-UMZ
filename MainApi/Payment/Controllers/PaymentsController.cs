namespace MainApi.Payment.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(IMediator mediator, ICurrentUserService currentUserService) : BaseApiController(currentUserService)
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("initiate")]
    [Authorize]
    public async Task<IActionResult> InitiatePayment([FromBody] PaymentInitiationDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new InitiatePaymentCommand(dto, CurrentUser.UserId.Value, CurrentUser.IpAddress ?? "Unknown");
        var result = await _mediator.Send(command);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("verify")]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        var command = new VerifyPaymentCommand(authority, status);
        var result = await _mediator.Send(command);

        if (result.IsSuccess && result.Value?.RedirectUrl != null)
        {
            return Redirect(result.Value.RedirectUrl);
        }

        return Ok(result);
    }

    [HttpGet("{authority}")]
    [Authorize]
    public async Task<IActionResult> GetByAuthority(string authority)
    {
        var result = await _mediator.Send(new GetPaymentByAuthorityQuery(authority));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var result = await _mediator.Send(new GetPaymentsByOrderQuery(orderId));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("status/{authority}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(string authority)
    {
        var result = await _mediator.Send(new GetPaymentStatusQuery(authority));
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("webhook/{gateway}")]
    public async Task<IActionResult> Webhook(string gateway, [FromBody] WebhookPayload payload)
    {
        var command = new ProcessWebhookCommand(gateway, payload.Authority, payload.Status, payload.RefId);
        await _mediator.Send(command);
        return Ok();
    }
}