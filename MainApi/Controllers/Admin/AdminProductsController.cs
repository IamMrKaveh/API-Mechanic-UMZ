namespace MainApi.Controllers.Admin;

[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminProductsController(ICurrentUserService currentUserService, IMediator mediator)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchDto searchDto)
    {
        var query = new GetAdminProductsQuery(searchDto);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAdminProductById(int id)
    {
        var query = new GetAdminProductByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromForm] ProductDto productDto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();

        // تبدیل فایل‌های ورودی به DTO مستقل از فریم‌ورک
        if (Request.Form.Files.Count > 0)
        {
            productDto.Images = new List<FileDto>();
            foreach (var file in Request.Form.Files)
            {
                var fileDto = new FileDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    Content = file.OpenReadStream() // استریم باز می‌شود و در لایه Application مدیریت خواهد شد
                };
                productDto.Images.Add(fileDto);
            }
        }

        var command = new CreateProductCommand(productDto, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);

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

        // تبدیل فایل‌های ورودی
        if (Request.Form.Files.Count > 0)
        {
            productDto.Images = new List<FileDto>();
            foreach (var file in Request.Form.Files)
            {
                productDto.Images.Add(new FileDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    Content = file.OpenReadStream()
                });
            }
        }

        var command = new UpdateProductCommand(id, productDto, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var command = new DeleteProductCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        if (CurrentUser.UserId == null) return Unauthorized();
        var command = new RestoreProductCommand(id, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("stock/add")]
    public async Task<IActionResult> AddStock([FromBody] ProductStockDto stockDto)
    {
        if (stockDto.VariantId == null || CurrentUser.UserId == null)
            return BadRequest("VariantId and user authentication are required.");

        var command = new AddStockCommand(stockDto.VariantId.Value, stockDto.Quantity, CurrentUser.UserId.Value, stockDto.Notes);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("stock/remove")]
    public async Task<IActionResult> RemoveStock([FromBody] ProductStockDto stockDto)
    {
        if (stockDto.VariantId == null || CurrentUser.UserId == null)
            return BadRequest("VariantId and user authentication are required.");

        var command = new RemoveStockCommand(stockDto.VariantId.Value, stockDto.Quantity, CurrentUser.UserId.Value, stockDto.Notes);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("variants/{variantId:int}/discount")]
    public async Task<IActionResult> SetDiscount(int variantId, [FromBody] SetDiscountDto discountDto)
    {
        if (CurrentUser.UserId == null) return Unauthorized();

        var command = new SetDiscountCommand(variantId, discountDto.OriginalPrice, discountDto.DiscountedPrice, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("variants/{variantId:int}/discount")]
    public async Task<IActionResult> RemoveDiscount(int variantId)
    {
        if (CurrentUser.UserId == null) return Unauthorized();

        var command = new RemoveDiscountCommand(variantId, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    // BulkUpdatePrices هنوز نیاز به Refactor دارد یا حذف شود اگر استفاده نمی‌شود
    // برای سادگی فعلا کامنت می‌شود تا بعدا Command مربوطه ساخته شود
    // [HttpPost("bulk-update-prices")] ...

    [HttpGet("statistics")]
    public async Task<IActionResult> GetProductStatistics()
    {
        var query = new GetProductStatisticsQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        var query = new GetLowStockProductsQuery(threshold);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("attributes/with-values")]
    public async Task<IActionResult> GetAttributesWithValues()
    {
        var query = new GetAllAttributesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}