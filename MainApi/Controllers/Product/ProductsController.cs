namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly ILogger<ProductsController> _logger;
    private readonly IStorageService _storageService;

    public ProductsController(
            MechanicContext context,
            ILogger<ProductsController> logger,
            IStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProducts([FromQuery] ProductSearchDto search)
    {
        if (search.Page < 1) search.Page = 1;
        if (search.PageSize < 1) search.PageSize = 10;
        if (search.PageSize > 100) search.PageSize = 100;

        try
        {
            var query = _context.TProducts.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                var pattern = $"%{search.Name}%";
                query = query.Where(p => p.Name != null && EF.Functions.Like(p.Name, pattern));
            }

            if (search.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == search.CategoryId);
            }

            if (search.MinPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice >= search.MinPrice.Value);
            }

            if (search.MaxPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice <= search.MaxPrice.Value);
            }

            if (search.InStock.HasValue && search.InStock.Value)
            {
                query = query.Where(p => (!p.IsUnlimited && p.Count > 0) || p.IsUnlimited);
            }

            var sortBy = Request.Query["sortBy"].ToString();
            if (string.IsNullOrWhiteSpace(sortBy)) sortBy = Request.Query["sort"].ToString();

            query = (sortBy?.ToLowerInvariant()) switch
            {
                "price_asc" => query.OrderBy(p => p.SellingPrice).ThenByDescending(p => p.Id),
                "price_desc" => query.OrderByDescending(p => p.SellingPrice).ThenByDescending(p => p.Id),
                "name_asc" => query.OrderBy(p => p.Name).ThenByDescending(p => p.Id),
                "name_desc" => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
                "discount_desc" => query.OrderByDescending(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) : 0).ThenByDescending(p => p.Id),
                "discount_asc" => query.OrderBy(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) : 0).ThenByDescending(p => p.Id),
                "oldest" => query.OrderBy(p => p.Id),
                "newest" => query.OrderByDescending(p => p.Id),
                _ => query.OrderByDescending(p => p.Id)
            };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .Select(p => new PublicProductViewDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Icon = string.IsNullOrEmpty(p.Icon) ? null : BaseUrl + p.Icon,
                    OriginalPrice = p.OriginalPrice,
                    SellingPrice = p.SellingPrice,
                    Count = p.Count,
                    IsUnlimited = p.IsUnlimited,
                    CategoryId = p.CategoryId,
                    Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null,
                })
                .ToListAsync();

            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / search.PageSize);

            return Ok(new
            {
                Items = items,
                TotalItems = totalItems,
                Page = search.Page,
                PageSize = search.PageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving products", Error = ex.Message });
        }
    }


    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }

        try
        {
            var productQuery = _context.TProducts
                .Include(p => p.Category)
                .Where(p => p.Id == id);

            var isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

            var product = await productQuery.FirstOrDefaultAsync();

            if (product == null) return NotFound(new { Message = $"Product with ID {id} not found" });

            if (isAdmin)
            {
                var adminDto = new AdminProductViewDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Icon = string.IsNullOrEmpty(product.Icon) ? null : BaseUrl + product.Icon,
                    PurchasePrice = product.PurchasePrice,
                    OriginalPrice = product.OriginalPrice,
                    SellingPrice = product.SellingPrice,
                    Count = product.Count,
                    IsUnlimited = product.IsUnlimited,
                    CategoryId = product.CategoryId,
                    Category = product.Category != null ? new { product.Category.Id, product.Category.Name } : null
                };
                return Ok(adminDto);
            }
            else
            {
                var publicDto = new PublicProductViewDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Icon = string.IsNullOrEmpty(product.Icon) ? null : BaseUrl + product.Icon,
                    OriginalPrice = product.OriginalPrice,
                    SellingPrice = product.SellingPrice,
                    Count = product.Count,
                    IsUnlimited = product.IsUnlimited,
                    CategoryId = product.CategoryId,
                    Category = product.Category != null ? new { product.Category.Id, product.Category.Name } : null
                };
                return Ok(publicDto);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving product", Error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CreateProduct([FromForm] ProductDto productDto)
    {
        if (productDto == null) return BadRequest(new { Message = "Product data is required" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (productDto.OriginalPrice > 0 && productDto.SellingPrice >= productDto.OriginalPrice) return BadRequest(new { Message = "Selling price must be less than original price when a discount is applied." });
        if (productDto.PurchasePrice > productDto.SellingPrice) return BadRequest(new { Message = "Purchase price cannot be greater than selling price." });

        string? iconUrl = null;
        if (productDto.IconFile != null)
        {
            iconUrl = await _storageService.UploadFileAsync(productDto.IconFile, "images/products");
        }

        var sanitizer = new HtmlSanitizer();
        var product = new TProducts
        {
            Name = sanitizer.Sanitize(productDto.Name),
            Icon = iconUrl,
            PurchasePrice = productDto.PurchasePrice,
            SellingPrice = productDto.SellingPrice,
            OriginalPrice = productDto.OriginalPrice,
            Count = productDto.IsUnlimited ? 0 : productDto.Count,
            IsUnlimited = productDto.IsUnlimited,
            CategoryId = productDto.CategoryId
        };

        _context.TProducts.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new
        {
            product.Id,
            product.Name,
            Icon = string.IsNullOrEmpty(product.Icon) ? null : BaseUrl + product.Icon,
            product.SellingPrice,
            product.OriginalPrice,
            product.Count,
            product.IsUnlimited,
            product.CategoryId
        });
    }

    [HttpPost("{id:int}/stock/add")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0)
            return BadRequest(new { Message = "Invalid product ID" });

        if (stockDto == null || !ModelState.IsValid)
            return BadRequest(new { Message = "Stock data is required and must be valid" });

        if (stockDto.Quantity <= 0 || stockDto.Quantity > 100000)
            return BadRequest(new { Message = "Quantity must be between 1 and 100000" });

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            if (product.IsUnlimited)
            {
                return BadRequest(new { Message = "Cannot change stock for an unlimited product." });
            }

            product.Count += stockDto.Quantity;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Stock added successfully", NewCount = product.Count });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Error adding stock", Error = ex.Message });
        }
    }

    [HttpPost("{id:int}/stock/remove")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0)
            return BadRequest(new { Message = "Invalid product ID" });

        if (stockDto == null || !ModelState.IsValid)
            return BadRequest(new { Message = "Stock data is required and must be valid" });

        if (stockDto.Quantity <= 0)
            return BadRequest(new { Message = "Quantity must be greater than zero" });

        using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            if (product.IsUnlimited)
            {
                return BadRequest(new { Message = "Cannot change stock for an unlimited product." });
            }

            if (product.Count < stockDto.Quantity)
            {
                return BadRequest(new { Message = $"Insufficient stock. Current stock: {product.Count}, Requested: {stockDto.Quantity}" });
            }

            product.Count -= stockDto.Quantity;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Stock removed successfully", NewCount = product.Count });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { Message = "The product was modified by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
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
            var products = await _context.TProducts
                .Include(p => p.Category)
                .Where(p => !p.IsUnlimited && p.Count <= threshold && p.Count > 0)
                .OrderBy(p => p.Count)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Count,
                    Category = p.Category != null ? p.Category.Name : null,
                    p.SellingPrice
                })
                .ToListAsync();

            return Ok(products);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving low stock products", Error = ex.Message });
        }
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetProductStatistics()
    {
        try
        {
            var totalProducts = await _context.TProducts.CountAsync();

            var totalValue = await _context.TProducts
                .Where(p => !p.IsUnlimited && p.Count > 0)
                .SumAsync(p => (decimal)p.Count * p.PurchasePrice);

            var outOfStockCount = await _context.TProducts
                .CountAsync(p => !p.IsUnlimited && p.Count == 0);

            var lowStockCount = await _context.TProducts
                .CountAsync(p => !p.IsUnlimited && p.Count <= 5 && p.Count > 0);

            return Ok(new
            {
                TotalProducts = totalProducts,
                TotalInventoryValue = (long)totalValue,
                OutOfStockProducts = outOfStockCount,
                LowStockProducts = lowStockCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving statistics", Error = ex.Message });
        }
    }

    [HttpPost("bulk-update-prices")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] Dictionary<int, int> priceUpdates, [FromQuery] bool isPurchasePrice = false)
    {
        if (priceUpdates == null || !priceUpdates.Any())
        {
            return BadRequest(new { Message = "Price updates data is required" });
        }

        var invalidPrices = priceUpdates.Where(p => p.Value <= 0).ToList();
        if (invalidPrices.Any())
        {
            return BadRequest(new { Message = "Prices must be greater than zero." });
        }

        try
        {
            var productIds = priceUpdates.Keys.ToList();
            var products = await _context.TProducts
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (!products.Any())
            {
                return NotFound(new { Message = "No products found with the provided IDs" });
            }

            var updatedCount = 0;
            foreach (var product in products)
            {
                if (priceUpdates.ContainsKey(product.Id))
                {
                    if (isPurchasePrice)
                        product.PurchasePrice = priceUpdates[product.Id];
                    else
                        product.SellingPrice = priceUpdates[product.Id];
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{updatedCount} products updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error updating prices", Error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] ProductDto productDto)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (productDto.OriginalPrice > 0 && productDto.SellingPrice >= productDto.OriginalPrice) return BadRequest(new { Message = "Selling price must be less than original price when a discount is applied." });
        if (productDto.PurchasePrice > productDto.SellingPrice) return BadRequest(new { Message = "Purchase price cannot be greater than selling price." });

        var existingProduct = await _context.TProducts.FindAsync(id);
        if (existingProduct == null) return NotFound(new { Message = $"Product with ID {id} not found" });

        if (productDto.RowVersion != null) _context.Entry(existingProduct).Property("RowVersion").OriginalValue = productDto.RowVersion;

        if (productDto.IconFile != null)
        {
            if (!string.IsNullOrEmpty(existingProduct.Icon))
            {
                await _storageService.DeleteFileAsync(existingProduct.Icon);
            }
            existingProduct.Icon = await _storageService.UploadFileAsync(productDto.IconFile, "images/products");
        }

        var sanitizer = new HtmlSanitizer();
        existingProduct.Name = sanitizer.Sanitize(productDto.Name);
        existingProduct.PurchasePrice = productDto.PurchasePrice;
        existingProduct.SellingPrice = productDto.SellingPrice;
        existingProduct.OriginalPrice = productDto.OriginalPrice;
        existingProduct.Count = productDto.IsUnlimited ? 0 : productDto.Count;
        existingProduct.IsUnlimited = productDto.IsUnlimited;
        existingProduct.CategoryId = productDto.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0) return BadRequest(new { Message = "Invalid product ID" });

        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return NotFound(new { Message = $"Product with ID {id} not found" });

        var hasOrderHistory = await _context.TOrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrderHistory) return BadRequest(new { Message = "Cannot delete product that has order history. Consider deactivating instead." });

        string? iconPath = product.Icon;

        _context.TProducts.Remove(product);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(iconPath))
        {
            await _storageService.DeleteFileAsync(iconPath);
        }

        return Ok(new { Message = "Product deleted successfully" });
    }

    [HttpGet("discounted")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetDiscountedProducts(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int minDiscount = 0,
    [FromQuery] int maxDiscount = 0,
    [FromQuery] int CategoryId = 0)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var query = _context.TProducts
                .Include(p => p.Category)
                .Where(p => p.OriginalPrice > p.SellingPrice &&
                           (p.Count > 0 || p.IsUnlimited))
                .AsQueryable();

            if (CategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == CategoryId);
            }

            if (minDiscount > 0)
            {
                query = query.Where(p => p.OriginalPrice > 0 &&
                    ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) >= minDiscount);
            }

            if (maxDiscount > 0)
            {
                query = query.Where(p => p.OriginalPrice > 0 &&
                    ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) <= maxDiscount);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) : 0)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Icon = string.IsNullOrEmpty(p.Icon) ? null : BaseUrl + p.Icon,
                    p.OriginalPrice,
                    p.SellingPrice,
                    DiscountAmount = p.OriginalPrice - p.SellingPrice,
                    DiscountPercentage = p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice) : 0,
                    p.Count,
                    p.IsUnlimited,
                    p.CategoryId,
                    Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null
                })
                .ToListAsync();

            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling((double)totalItems / pageSize);

            return Ok(new
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving discounted products", Error = ex.Message });
        }
    }

    [HttpPut("{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetProductDiscount(int id, [FromBody] SetDiscountDto discountDto)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }

        if (discountDto == null || !ModelState.IsValid)
        {
            return BadRequest(new { Message = "Invalid discount data" });
        }

        if (discountDto.DiscountedPrice >= discountDto.OriginalPrice)
        {
            return BadRequest(new { Message = "Discounted price must be less than original price" });
        }

        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }

            product.OriginalPrice = discountDto.OriginalPrice;
            product.SellingPrice = discountDto.DiscountedPrice;

            await _context.SaveChangesAsync();

            var discountPercentage = ((discountDto.OriginalPrice - discountDto.DiscountedPrice) * 100.0 / discountDto.OriginalPrice);

            return Ok(new
            {
                Message = "Discount applied successfully",
                DiscountPercentage = Math.Round((decimal)discountPercentage, 2),
                OriginalPrice = product.OriginalPrice,
                DiscountedPrice = product.SellingPrice
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error applying discount", Error = ex.Message });
        }
    }

    [HttpDelete("{id:int}/discount")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveProductDiscount(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }
        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with ID {id} not found" });
            }
            if (product.OriginalPrice <= product.SellingPrice)
            {
                return BadRequest(new { Message = "Product does not have a valid discount to remove" });
            }
            product.SellingPrice = product.OriginalPrice;
            product.OriginalPrice = 0;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Discount removed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error removing discount", Error = ex.Message });
        }
    }

    [HttpGet("discount-statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetDiscountStatistics()
    {
        try
        {
            var totalDiscountedProducts = await _context.TProducts
                .CountAsync(p => p.OriginalPrice > p.SellingPrice);

            var averageDiscountPercentage = await _context.TProducts
                .Where(p => p.OriginalPrice > 0 &&
                           p.OriginalPrice > p.SellingPrice)
                .Select(p => ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice))
                .DefaultIfEmpty(0)
                .AverageAsync();

            var totalDiscountValue = await _context.TProducts
                .Where(p => p.OriginalPrice > p.SellingPrice &&
                           !p.IsUnlimited && p.Count > 0)
                .SumAsync(p => (long)(p.OriginalPrice - p.SellingPrice) * p.Count);

            var discountByCategory = await _context.TProducts
                .Include(p => p.Category)
                .Where(p => p.OriginalPrice > 0 && p.OriginalPrice > p.SellingPrice)
                .GroupBy(p => new { p.CategoryId, p.Category!.Name })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Count = g.Count(),
                    AverageDiscount = g.Average(p => ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / p.OriginalPrice))
                })
                .ToListAsync();

            return Ok(new
            {
                TotalDiscountedProducts = totalDiscountedProducts,
                AverageDiscountPercentage = Math.Round(averageDiscountPercentage, 2),
                TotalDiscountValue = totalDiscountValue,
                DiscountByCategory = discountByCategory
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Error retrieving discount statistics", Error = ex.Message });
        }
    }
}