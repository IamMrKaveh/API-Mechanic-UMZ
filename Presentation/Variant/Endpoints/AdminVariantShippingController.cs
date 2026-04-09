using Application.Shipping.Features.Queries.GetShippings;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Queries.GetProductVariantShipping;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public class AdminVariantShippingController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetShippings()
    {
        var query = new GetShippingsQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{variantId}")]
    public async Task<IActionResult> GetVariantShipping(Guid variantId)
    {
        var result = await _mediator.Send(new GetVariantShippingQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPut("{variantId}")]
    public async Task<IActionResult> UpdateShippings(
        Guid variantId,
        [FromBody] UpdateVariantShippingRequest request)
    {
        var command = new UpdateVariantShippingCommand(
            variantId,
            request.ShippingMultiplier,
            request.EnabledShippingIds,
            CurrentUser.UserId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}