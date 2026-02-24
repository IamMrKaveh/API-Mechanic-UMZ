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
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new UpdateProductVariantShippingCommand
        {
            VariantId = variantId,
            ShippingMultiplier = dto.ShippingMultiplier,
            EnabledShippingIds = dto.EnabledShippingIds,
            UserId = CurrentUser.UserId.Value
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}