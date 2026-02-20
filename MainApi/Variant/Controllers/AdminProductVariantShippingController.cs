using Application.Shipping.Features.Shared;

namespace MainApi.Variant.Controllers;

[Route("api/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public class AdminProductVariantShippingController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminProductVariantShippingController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods()
    {
        var query = new GetAllShippingQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateShippingMethods(
        int variantId,
        [FromBody] UpdateProductVariantShippingMethodsDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new UpdateProductVariantShippingCommand
        {
            VariantId = variantId,
            ShippingMultiplier = dto.ShippingMultiplier,
            EnabledShippingMethodIds = dto.EnabledShippingMethodIds,
            UserId = CurrentUser.UserId.Value
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("all-methods")]
    public async Task<IActionResult> GetAllShippingMethods()
    {
        // استفاده مجدد از Shipping Query
        var query = new GetAllShippingQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}