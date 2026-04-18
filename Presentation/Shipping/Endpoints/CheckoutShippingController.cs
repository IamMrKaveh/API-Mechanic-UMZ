using Application.Shipping.Features.Queries.CalculateShippingCost;
using Application.Shipping.Features.Queries.GetAvailableShippings;
using Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

namespace Presentation.Shipping.Endpoints;

[Route("api/checkout/shipping")]
[ApiController]
[Authorize]
public sealed class CheckoutShippingController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("available-methods")]
    public async Task<IActionResult> GetAvailableShippings(
        [FromQuery] decimal orderAmount,
        CancellationToken ct)
    {
        var query = new GetAvailableShippingsQuery(orderAmount);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("available-methods-for-variants")]
    public async Task<IActionResult> GetAvailableShippingsForVariants(
        [FromBody] ICollection<Guid> variantIds,
        CancellationToken ct)
    {
        var result = await Mediator.Send(
            new GetAvailableShippingsForVariantsQuery(variantIds), ct);
        return ToActionResult(result);
    }

    [HttpGet("calculate")]
    public async Task<IActionResult> CalculateShippingCost(
        [FromQuery] Guid shippingId,
        [FromQuery] decimal orderAmount,
        CancellationToken ct)
    {
        var query = new CalculateShippingCostQuery(shippingId, orderAmount);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}