using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/shipping")]
[AllowAnonymous]
public sealed class ShippingController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShippingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveShippings(CancellationToken ct)
    {
        var query = new GetShippingsQuery(false);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}