using Application.Shipping.Features.Queries.GetShippings;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Queries.GetVariantShipping;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public sealed class AdminVariantShippingController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetShippings(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetShippingsQuery(false), ct);
        return ToActionResult(result);
    }

    [HttpGet("{variantId:guid}")]
    public async Task<IActionResult> GetVariantShipping(Guid variantId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetVariantShippingQuery(variantId), ct);
        return ToActionResult(result);
    }

    [HttpPut("{variantId:guid}")]
    public async Task<IActionResult> UpdateShippings(
        Guid variantId,
        [FromBody] UpdateVariantShippingRequest request,
        CancellationToken ct)
    {
        var command = new UpdateVariantShippingCommand(
            variantId,
            request.ShippingMultiplier,
            request.EnabledShippingIds,
            CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}