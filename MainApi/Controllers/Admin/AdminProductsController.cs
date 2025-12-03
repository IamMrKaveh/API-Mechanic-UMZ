using Application.Common.Interfaces.Admin;

namespace MainApi.Controllers.Admin;

[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : BaseApiController
{
    private readonly IAdminProductService _adminProductService;

    public AdminProductsController(IAdminProductService adminProductService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _adminProductService = adminProductService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAdminProductById(int id)
    {
        var result = await _adminProductService.GetAdminProductByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromForm] ProductDto productDto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var result = await _adminProductService.CreateProductAsync(productDto, CurrentUser.UserId.Value);
        if (result.Success && result.Data != null)
        {
            return CreatedAtAction(nameof(GetAdminProductById), new { id = result.Data.Id }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto productDto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var result = await _adminProductService.UpdateProductAsync(id, productDto, CurrentUser.UserId.Value);
        return ToActionResult(result);
    }

    [HttpPost("stock/add")]
    public async Task<IActionResult> AddStock([FromBody] ProductStockDto stockDto)
    {
        if (stockDto.VariantId == null || CurrentUser.UserId == null) return BadRequest("VariantId and user authentication are required.");
        var result = await _adminProductService.AddStockAsync(stockDto.VariantId.Value, stockDto.Quantity, CurrentUser.UserId.Value, stockDto.Notes ?? string.Empty);
        return ToActionResult(result);
    }

    [HttpPost("stock/remove")]
    public async Task<IActionResult> RemoveStock([FromBody] ProductStockDto stockDto)
    {
        if (stockDto.VariantId == null || CurrentUser.UserId == null) return BadRequest("VariantId and user authentication are required.");
        var result = await _adminProductService.RemoveStockAsync(stockDto.VariantId.Value, stockDto.Quantity, CurrentUser.UserId.Value, stockDto.Notes ?? string.Empty);
        return ToActionResult(result);
    }

    [HttpPut("variants/{variantId:int}/discount")]
    public async Task<IActionResult> SetDiscount(int variantId, [FromBody] SetDiscountDto discountDto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var result = await _adminProductService.SetDiscountAsync(variantId, discountDto.OriginalPrice, discountDto.DiscountedPrice, CurrentUser.UserId.Value);
        return ToActionResult(result);
    }

    [HttpDelete("variants/{variantId:int}/discount")]
    public async Task<IActionResult> RemoveDiscount(int variantId)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var result = await _adminProductService.RemoveDiscountAsync(variantId, CurrentUser.UserId.Value);
        return ToActionResult(result);
    }

    [HttpPost("bulk-update-prices")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] Dictionary<int, decimal> priceUpdates, [FromQuery] bool isPurchasePrice = false)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var result = await _adminProductService.BulkUpdatePricesAsync(priceUpdates, isPurchasePrice, CurrentUser.UserId.Value);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetProductStatistics()
    {
        var result = await _adminProductService.GetProductStatisticsAsync();
        return ToActionResult(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        var result = await _adminProductService.GetLowStockProductsAsync(threshold);
        return ToActionResult(result);
    }
}