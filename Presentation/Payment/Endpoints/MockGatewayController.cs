using System.Text.Encodings.Web;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/mock-gateway")]
public sealed class MockGatewayController(
    IWebHostEnvironment env,
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Index([FromQuery] string orderId, [FromQuery] decimal amount)
    {
        // Never served outside Development: a mock gateway in staging/production
        // would let anyone forge a payment callback simulation.
        if (!env.IsDevelopment())
            return NotFound();

        // SEC-23: encode client-supplied values for both HTML text and attribute
        // contexts (orderId is attacker-controlled query input).
        var encodedOrderId = HtmlEncoder.Default.Encode(orderId);

        var html = $"""
            <html>
            <body>
                <h2>Mock Payment Gateway</h2>
                <p>Order: {encodedOrderId} | Amount: {amount}</p>
                <form method="post" action="/api/payments/mock/callback">
                    <input type="hidden" name="orderId" value="{encodedOrderId}" />
                    <button type="submit" name="result" value="success">Pay</button>
                    <button type="submit" name="result" value="failure">Fail</button>
                </form>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}
