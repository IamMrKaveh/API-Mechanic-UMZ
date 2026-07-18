using Application.Shipping.Features.Queries.CalculateShippingCost;
using Application.Shipping.Features.Queries.GetAvailableShippings;
using Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;
using Application.Shipping.Features.Queries.GetShippingQuotes;
using Application.Shipping.Features.Shared;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/checkout/shipping")]
[Authorize]
public sealed class CheckoutShippingController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("available")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailableShippingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableShippings(
        [FromQuery] decimal orderAmount = 0m,
        CancellationToken ct = default)
    {
        var safeAmount = orderAmount < 0 ? 0m : orderAmount;
        var query = new GetAvailableShippingsQuery(safeAmount);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("cost")]
    [ProducesResponseType(typeof(ApiResponse<ShippingCostResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateShippingCost(
        [FromQuery] Guid shippingId,
        [FromQuery] decimal orderAmount,
        CancellationToken ct)
    {
        var safeAmount = orderAmount < 0 ? 0m : orderAmount;
        var query = new CalculateShippingCostQuery(shippingId, safeAmount);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("available")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailableShippingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableShippingsForVariants(
        [FromBody] ICollection<Guid> variantIds,
        CancellationToken ct)
    {
        var query = new GetAvailableShippingsForVariantsQuery(variantIds ?? Array.Empty<Guid>());
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("quotes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AvailableShippingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippingQuotes(
        [FromBody] GetShippingQuotesQuery query,
        CancellationToken ct)
    {
        if (query is null)
            return ToActionResult(ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success([]));

        if (query.OrderAmount < 0 || query.Items is null || query.Items.Count == 0)
        {
            var fallback = new GetAvailableShippingsQuery(query?.OrderAmount < 0 ? 0m : query!.OrderAmount);
            var fallbackResult = await Mediator.Send(fallback, ct);
            return ToActionResult(fallbackResult);
        }

        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}