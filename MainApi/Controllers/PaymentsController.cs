namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        var result = await _paymentService.VerifyPaymentAsync(authority, status);
        return Ok(result);
    }

    //[HttpPost("webhook/zarinpal")]
    //[AllowAnonymous]
    //[IgnoreAntiforgeryToken]
    //public async Task<IActionResult> ZarinpalWebhook([FromBody] JsonElement payload)
    //{
    //    try
    //    {
    //        if (payload.TryGetProperty("Authority", out var authProp) &&
    //            payload.TryGetProperty("Status", out var statusProp))
    //        {
    //            var authority = authProp.GetString();
    //            var status = statusProp.GetString();
    //            long? refId = payload.TryGetProperty("RefID", out var refProp) ? refProp.GetInt64() : null;

    //            if (!string.IsNullOrEmpty(authority) && !string.IsNullOrEmpty(status))
    //            {
    //                await _paymentService.ProcessZarinPalWebhookAsync(authority, status, refId);
    //                return Ok();
    //            }
    //        }
    //        return BadRequest("Invalid Webhook Payload");
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Webhook processing error");
    //        return StatusCode(500);
    //    }
    //}
}