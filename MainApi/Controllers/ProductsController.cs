namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;
    private readonly ILogger<ProductsController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ProductsController(IProductService productService, IReviewService reviewService, ILogger<ProductsController> logger, ICurrentUserService currentUserService)
    {
        _productService = productService;
        _reviewService = reviewService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchDto search)
    {
        var result = await _productService.GetProductsAsync(search);
        if (!result.Success)
        {
            _logger.LogError("Error retrieving products: {Error}", result.Error);
            return StatusCode(500, new { Message = "Error retrieving products" });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });

        var isAdmin = _currentUserService.IsAdmin;
        var result = await _productService.GetProductByIdAsync(id, isAdmin);

        if (!result.Success) return NotFound(new { Message = result.Error });
        if (result.Data == null) return NotFound(new { Message = $"Product with ID {id} not found" });

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductDto productDto)
    {
        if (productDto == null) return BadRequest(new { Message = "Product data is required" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var createResult = await _productService.CreateProductAsync(productDto, userId.Value);
        if (!createResult.Success || createResult.Data == null)
        {
            _logger.LogError("Error creating product: {Error}", createResult.Error);
            return StatusCode(500, new { Message = "Error creating product" });
        }

        var getResult = await _productService.GetProductByIdAsync(createResult.Data.Id, true);
        return CreatedAtAction(nameof(GetProduct), new { id = createResult.Data.Id }, getResult.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto productDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _productService.UpdateProductAsync(id, productDto, userId.Value);

        if (!result.Success)
        {
            if (result.Error == "Product not found")
                return NotFound(new { Message = result.Error });

            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });

        var result = await _productService.DeleteProductAsync(id);
        if (!result.Success)
        {
            if (result.Error != null && result.Error.Contains("not found"))
                return NotFound(new { Message = result.Error });
            return BadRequest(new { Message = result.Error });
        }
        return Ok(new { Message = "Product deleted successfully" });
    }

    [HttpPost("{id:int}/stock/add")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (stockDto == null || !ModelState.IsValid || stockDto.Quantity <= 0 || stockDto.Quantity > 100000)
            return BadRequest(new { Message = "Stock data is required and quantity must be between 1 and 100000" });
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _productService.AddStockAsync(id, stockDto, userId.Value);
        if (!result.Success || result.Data.newCount == null)
        {
            if (result.Error != null && result.Error.Contains("not found"))
                return NotFound(new { Message = result.Error });
            return BadRequest(new { Message = result.Error });
        }

        return Ok(new { Message = result.Data.message, NewCount = result.Data.newCount });
    }

    [HttpPost("{id:int}/stock/remove")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (stockDto == null || !ModelState.IsValid || stockDto.Quantity <= 0)
            return BadRequest(new { Message = "Stock data is required and quantity must be greater than zero." });
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _productService.RemoveStockAsync(id, stockDto, userId.Value);
        if (!result.Success || result.Data.newCount == null)
        {
            if (result.Error != null && result.Error.Contains("not found"))
                return NotFound(new { Message = result.Error });
            return BadRequest(new { Message = result.Error });
        }

        return Ok(new { Message = result.Data.message, NewCount = result.Data.newCount });
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        if (threshold < 0) threshold = 5;
        var result = await _productService.GetLowStockProductsAsync(threshold);
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving low stock products" });

        return Ok(result.Data);
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetProductStatistics()
    {
        var result = await _productService.GetProductStatisticsAsync();
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving statistics" });

        return Ok(result.Data);
    }

    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] Dictionary<int, decimal> priceUpdates, [FromQuery] bool isPurchasePrice = false)
    {
        if (priceUpdates == null || !priceUpdates.Any())
            return BadRequest(new { Message = "Price updates data is required" });
        if (priceUpdates.Any(p => p.Value <= 0))
            return BadRequest(new { Message = "Prices must be greater than zero." });

        var result = await _productService.BulkUpdatePricesAsync(priceUpdates, isPurchasePrice);
        if (!result.Success)
            return NotFound(new { Message = result.Error });

        return Ok(new { Message = result.Data.message });
    }

    [HttpGet("discounted")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDiscountedProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int minDiscount = 0, [FromQuery] int maxDiscount = 0, [FromQuery] int categoryId = 0)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _productService.GetDiscountedProductsAsync(page, pageSize, minDiscount, maxDiscount, categoryId);
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving discounted products" });

        return Ok(result.Data);
    }

    [HttpPut("variants/{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetProductDiscount(int id, [FromBody] SetDiscountDto discountDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid variant ID" });
        if (discountDto == null || !ModelState.IsValid || discountDto.DiscountedPrice >= discountDto.OriginalPrice)
            return BadRequest(new { Message = "Invalid discount data. Discounted price must be less than original price." });

        var result = await _productService.SetProductDiscountAsync(id, discountDto);
        if (!result.Success || result.Data.result == null)
            return NotFound(new { Message = result.Error });

        return Ok(result.Data.result);
    }

    [HttpDelete("variants/{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveProductDiscount(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid variant ID" });

        var result = await _productService.RemoveProductDiscountAsync(id);
        if (!result.Success)
        {
            if (result.Error != null && result.Error.Contains("not found"))
                return NotFound(new { Message = result.Error });
            return BadRequest(new { Message = result.Error });
        }
        return Ok(new { Message = result.Error });
    }

    [HttpGet("discount-statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDiscountStatistics()
    {
        var result = await _productService.GetDiscountStatisticsAsync();
        if (!result.Success) return StatusCode(500, new { Message = "Error retrieving discount statistics" });
        return Ok(result.Data);
    }

    [HttpGet("{id:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductReviewDto>>> GetProductReviews(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (id <= 0) return BadRequest("Invalid product ID");
        var reviews = await _reviewService.GetProductReviewsAsync(id, page, pageSize);
        return Ok(reviews);
    }

    [HttpPost("reviews")]
    [Authorize]
    public async Task<ActionResult<ProductReviewDto>> CreateReview([FromBody] CreateReviewDto createReviewDto)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var review = await _reviewService.CreateReviewAsync(createReviewDto, userId.Value);
        return CreatedAtAction(nameof(GetProductReviews), new { id = review.Data!.ProductId }, review);
    }
}