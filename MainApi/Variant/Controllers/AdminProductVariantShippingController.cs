using Application.Shipping.Features.Shared;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Queries.GetProductVariantShipping;
using Presentation.Base.Controllers.v1;

namespace Presentation.Variant.Controllers;

[ApiController]
[Route("api/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public class AdminProductVariantShippingController(IMediator mediator) : BaseApiController(mediator)
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
    public async Task<IActionResult> GetVariantShipping(int variantId)
    {
        var result = await _mediator.Send(new GetProductVariantShippingQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateShippings(
        int variantId,
        [FromBody] UpdateProductVariantShippingsDto dto)
    {
        var command = new UpdateProductVariantShippingCommand
        {
            VariantId = variantId,
            ShippingMultiplier = dto.ShippingMultiplier,
            EnabledShippingIds = dto.EnabledShippingIds,
            UserId = CurrentUser.UserId
        };
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}