namespace MainApi.Controllers.Admin;

[Route("api/admin/products/variants/shipping")]
[Authorize(Roles = "Admin")]
public class AdminProductVariantShippingController : BaseApiController
{
    private readonly IAdminProductVariantShippingService _service;

    public AdminProductVariantShippingController(
        IAdminProductVariantShippingService service,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods(int variantId)
    {
        var result = await _service.GetShippingMethodsAsync(variantId);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateShippingMethods(
        int variantId,
        [FromBody] UpdateProductVariantShippingMethodsDto dto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();

        dto.ProductVariantId = variantId;
        var result = await _service.UpdateShippingMethodsAsync(
            variantId, dto, CurrentUser.UserId.Value);
        return ToActionResult(result);
    }

    [HttpGet("all-methods")]
    public async Task<IActionResult> GetAllShippingMethods()
    {
        var result = await _service.GetAllShippingMethodsAsync();
        return ToActionResult(result);
    }
}