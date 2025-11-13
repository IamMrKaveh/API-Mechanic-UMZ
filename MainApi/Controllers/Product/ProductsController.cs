using MainApi.Services.Product;

namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, IReviewService reviewService, ILogger<ProductsController> logger, IConfiguration configuration) : base(configuration)
    {
        _productService = productService;
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProducts([FromQuery] ProductSearchDto search)
    {
        try
        {
            var (products, totalItems) = await _productService.GetProductsAsync(search);
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / search.PageSize);

            return Ok(new
            {
                Items = products,
                TotalItems = totalItems,
                Page = search.Page,
                PageSize = search.PageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new { Message = "Error retrieving products", Error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProduct(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        try
        {
            var isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");
            var product = await _productService.GetProductByIdAsync(id, isAdmin);
            if (product == null) return NotFound(new { Message = $"Product with ID {id} not found" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
            return StatusCode(500, new { Message = "Error retrieving product", Error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CreateProduct([FromForm] ProductDto productDto)
    {
        if (productDto == null) return BadRequest(new { Message = "Product data is required" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var product = await _productService.CreateProductAsync(productDto, userId.Value);
            var result = await _productService.GetProductByIdAsync(product.Id, true);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product.");
            return StatusCode(500, new { Message = "Error creating product", Error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto productDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var success = await _productService.UpdateProductAsync(id, productDto, userId.Value);
            if (!success) return NotFound(new { Message = $"Product with ID {id} not found" });

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
            return StatusCode(500, new { Message = "Error updating product", Error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });

        try
        {
            var (success, message) = await _productService.DeleteProductAsync(id);
            if (!success)
            {
                if (message != null && message.Contains("not found"))
                    return NotFound(new { Message = message });
                return BadRequest(new { Message = message });
            }
            return Ok(new { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
            return StatusCode(500, new { Message = "Error deleting product", Error = ex.Message });
        }
    }

    [HttpPost("{id:int}/stock/add")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (stockDto == null || !ModelState.IsValid || stockDto.Quantity <= 0 || stockDto.Quantity > 100000)
            return BadRequest(new { Message = "Stock data is required and quantity must be between 1 and 100000" });
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var (success, newCount, message) = await _productService.AddStockAsync(id, stockDto, userId.Value);
            if (!success)
            {
                if (message != null && message.Contains("not found"))
                    return NotFound(new { Message = message });
                return BadRequest(new { Message = message });
            }
            return Ok(new { Message = message, NewCount = newCount });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock for product ID {ProductId}", id);
            return StatusCode(500, new { Message = "Error adding stock", Error = ex.Message });
        }
    }

    [HttpPost("{id:int}/stock/remove")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (stockDto == null || !ModelState.IsValid || stockDto.Quantity <= 0)
            return BadRequest(new { Message = "Stock data is required and quantity must be greater than zero." });
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var (success, newCount, message) = await _productService.RemoveStockAsync(id, stockDto, userId.Value);
            if (!success)
            {
                if (message != null && message.Contains("not found"))
                    return NotFound(new { Message = message });
                return BadRequest(new { Message = message });
            }
            return Ok(new { Message = message, NewCount = newCount });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing stock for product ID {ProductId}", id);
            return StatusCode(500, new { Message = "Error removing stock", Error = ex.Message });
        }
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        if (threshold < 0) threshold = 5;
        try
        {
            var products = await _productService.GetLowStockProductsAsync(threshold);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products");
            return StatusCode(500, new { Message = "Error retrieving low stock products", Error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetProductStatistics()
    {
        try
        {
            var stats = await _productService.GetProductStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product statistics.");
            return StatusCode(500, new { Message = "Error retrieving statistics", Error = ex.Message });
        }
    }

    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] Dictionary<int, decimal> priceUpdates, [FromQuery] bool isPurchasePrice = false)
    {
        if (priceUpdates == null || !priceUpdates.Any())
            return BadRequest(new { Message = "Price updates data is required" });
        if (priceUpdates.Any(p => p.Value <= 0))
            return BadRequest(new { Message = "Prices must be greater than zero." });

        try
        {
            var (updatedCount, message) = await _productService.BulkUpdatePricesAsync(priceUpdates, isPurchasePrice);
            if (updatedCount == 0 && message != null)
                return NotFound(new { Message = message });

            return Ok(new { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk price update.");
            return StatusCode(500, new { Message = "Error updating prices", Error = ex.Message });
        }
    }

    [HttpGet("discounted")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetDiscountedProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int minDiscount = 0,
        [FromQuery] int maxDiscount = 0,
        [FromQuery] int categoryId = 0)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var (products, totalItems) = await _productService.GetDiscountedProductsAsync(page, pageSize, minDiscount, maxDiscount, categoryId);
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / pageSize);

            return Ok(new
            {
                Items = products,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discounted products.");
            return StatusCode(500, new { Message = "Error retrieving discounted products", Error = ex.Message });
        }
    }

    [HttpPut("variants/{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetProductDiscount(int id, [FromBody] SetDiscountDto discountDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid variant ID" });
        if (discountDto == null || !ModelState.IsValid || discountDto.DiscountedPrice >= discountDto.OriginalPrice)
            return BadRequest(new { Message = "Invalid discount data. Discounted price must be less than original price." });

        try
        {
            var (success, result, message) = await _productService.SetProductDiscountAsync(id, discountDto);
            if (!success)
                return NotFound(new { Message = message });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount for variant ID {VariantId}", id);
            return StatusCode(500, new { Message = "Error applying discount", Error = ex.Message });
        }
    }

    [HttpDelete("variants/{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveProductDiscount(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid variant ID" });

        try
        {
            var (success, message) = await _productService.RemoveProductDiscountAsync(id);
            if (!success)
            {
                if (message != null && message.Contains("not found"))
                    return NotFound(new { Message = message });
                return BadRequest(new { Message = message });
            }
            return Ok(new { Message = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing discount for variant ID {VariantId}", id);
            return StatusCode(500, new { Message = "Error removing discount", Error = ex.Message });
        }
    }

    [HttpGet("discount-statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetDiscountStatistics()
    {
        try
        {
            var stats = await _productService.GetDiscountStatisticsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving discount statistics.");
            return StatusCode(500, new { Message = "Error retrieving discount statistics", Error = ex.Message });
        }
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
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var review = await _reviewService.CreateReviewAsync(createReviewDto, userId.Value);
            return CreatedAtAction(nameof(GetProductReviews), new { id = review.ProductId }, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for product {ProductId}", createReviewDto.ProductId);
            return StatusCode(500, "An error occurred while creating the review.");
        }
    }
}