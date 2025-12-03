using System.Text.RegularExpressions;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    private static readonly Regex AuthorityRegex = new(@"^[A-Za-z0-9\-_]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex StatusRegex = new(@"^(OK|NOK|Pending|Cancelled|Failed)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment([FromQuery] string authority, [FromQuery] string status)
    {
        var sanitizedAuthority = SanitizeAuthority(authority);
        var sanitizedStatus = SanitizeStatus(status);

        if (!IsValidAuthority(sanitizedAuthority))
        {
            _logger.LogWarning("Invalid authority format received: {Authority}", authority);
            return BadRequest(new { message = "Invalid authority format." });
        }

        if (!IsValidStatus(sanitizedStatus))
        {
            _logger.LogWarning("Invalid status format received: {Status}", status);
            return BadRequest(new { message = "Invalid status format." });
        }

        var result = await _paymentService.VerifyPaymentAsync(sanitizedAuthority, sanitizedStatus);
        return Redirect(result.RedirectUrl);
    }

    [HttpPost("webhook/zarinpal")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ZarinpalWebhook([FromBody] JsonElement payload, [FromQuery] string secret)
    {
        try
        {
            if (payload.TryGetProperty("Authority", out var authProp) &&
                payload.TryGetProperty("Status", out var statusProp))
            {
                var authority = authProp.GetString();
                var status = statusProp.GetString();
                long? refId = payload.TryGetProperty("RefID", out var refProp) ? refProp.GetInt64() : null;

                var sanitizedAuthority = SanitizeAuthority(authority);
                var sanitizedStatus = SanitizeStatus(status);

                if (!string.IsNullOrEmpty(sanitizedAuthority) &&
                    !string.IsNullOrEmpty(sanitizedStatus) &&
                    IsValidAuthority(sanitizedAuthority) &&
                    IsValidStatus(sanitizedStatus))
                {
                    await _paymentService.ProcessGatewayWebhookAsync("ZarinPal", sanitizedAuthority, sanitizedStatus, refId);
                    return Ok();
                }
            }

            _logger.LogWarning("Invalid webhook payload received");
            return BadRequest("Invalid Payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing error");
            return StatusCode(500);
        }
    }

    private static string SanitizeAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return string.Empty;

        var sanitized = authority.Trim();
        sanitized = sanitized.Replace("<", "").Replace(">", "").Replace("\"", "").Replace("'", "");

        if (sanitized.Length > 100)
            sanitized = sanitized.Substring(0, 100);

        return sanitized;
    }

    private static string SanitizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        var sanitized = status.Trim();
        sanitized = sanitized.Replace("<", "").Replace(">", "").Replace("\"", "").Replace("'", "");

        if (sanitized.Length > 20)
            sanitized = sanitized.Substring(0, 20);

        return sanitized;
    }

    private static bool IsValidAuthority(string authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return false;

        return AuthorityRegex.IsMatch(authority);
    }

    private static bool IsValidStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return false;

        return StatusRegex.IsMatch(status);
    }
}