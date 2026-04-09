using Application.Shipping.Features.Queries.CalculateShippingCost;
using Application.Shipping.Features.Queries.GetAvailableShippings;
using Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

namespace Presentation.Shipping.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class CheckoutShippingController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("available-methods")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippings()
    {
        var query = new GetAvailableShippingsQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("available-methods-for-variants")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippingsForVariants([FromBody] ICollection<Guid> variantIds)
    {
        var result = await _mediator.Send(new GetAvailableShippingsForVariantsQuery(variantIds));
        return ToActionResult(result);
    }

    [HttpGet("calculate")]
    [Authorize]
    public async Task<IActionResult> CalculateShippingCost([FromQuery] Guid shippingMethodId)
    {
        var query = new CalculateShippingCostQuery(CurrentUser.UserId, shippingMethodId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}