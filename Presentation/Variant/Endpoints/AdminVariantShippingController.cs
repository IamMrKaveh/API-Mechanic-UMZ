using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Queries.GetVariantShipping;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public sealed class AdminVariantShippingController(
    IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShippingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippings(CancellationToken ct)
    {
        var query = new GetShippingsQuery(false);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductVariantShippingInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVariantShipping(Guid variantId, CancellationToken ct)
    {
        var query = new GetVariantShippingQuery(variantId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPut("{variantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateShippings(
        Guid variantId,
        [FromBody] UpdateVariantShippingRequest request,
        CancellationToken ct)
    {
        var command = new UpdateVariantShippingCommand(
            variantId,
            request.ShippingMultiplier,
            request.EnabledShippingIds);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}