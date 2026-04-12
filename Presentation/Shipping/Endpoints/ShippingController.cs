using Application.Shipping.Features.Queries.GetShippings;

namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/shipping")]
[AllowAnonymous]
public sealed class ShippingController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetActiveShippings(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetShippingsQuery(false), ct);
        return ToActionResult(result);
    }
}