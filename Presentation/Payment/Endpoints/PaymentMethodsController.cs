using Application.Payment.Features.Queries.GetActivePaymentMethods;
using Application.Payment.Features.Shared;

namespace Presentation.Payment.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/payment-methods")]
[AllowAnonymous]
public sealed class PaymentMethodsController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailablePaymentMethodDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivePaymentMethods(
        [FromQuery] decimal orderAmount = 0m,
        CancellationToken ct = default)
    {
        var query = new GetActivePaymentMethodsQuery(orderAmount);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}