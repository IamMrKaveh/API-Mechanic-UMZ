using MapsterMapper;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/mock-gateway")]
public sealed class MockGatewayController(
    IWebHostEnvironment env,
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public IActionResult Index([FromQuery] string orderId, [FromQuery] decimal amount)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var html = $"""
            <html>
            <body>
                <h2>Mock Payment Gateway</h2>
                <p>Order: {orderId} | Amount: {amount}</p>
                <form method="post" action="/api/payments/mock/callback">
                    <input type="hidden" name="orderId" value="{orderId}" />
                    <button type="submit" name="result" value="success">Pay</button>
                    <button type="submit" name="result" value="failure">Fail</button>
                </form>
            </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}