namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly MechanicContext _context;

    public ProductsController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProducts([FromQuery] ProductSearchDto? search = null)
    {
        search ??= new ProductSearchDto();

        if (search.Page < 1) search.Page = 1;
        if (search.PageSize < 1) search.PageSize = 10;
        if (search.PageSize > 100) search.PageSize = 100;

        var query = _context.TProducts.Include(p => p.ProductType).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(p => p.Name != null && p.Name.ToLower().Contains(search.Name.ToLower()));
        }

        if (search.ProductTypeId.HasValue)
        {
            query = query.Where(p => p.ProductTypeId == search.ProductTypeId);
        }

        if (search.MinPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice >= search.MinPrice);
        }

        if (search.MaxPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice <= search.MaxPrice);
        }

        if (search.InStock.HasValue)
        {
            if (search.InStock.Value)
                query = query.Where(p => p.Count > 0);
            else
                query = query.Where(p => p.Count == 0 || p.Count == null);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Id)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Icon,
                p.PurchasePrice,
                p.SellingPrice,
                p.Count,
                p.ProductTypeId,
                ProductType = p.ProductType != null ? new { p.ProductType.Id, p.ProductType.Name } : null,
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

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        if (threshold < 0) threshold = 5;

        var products = await _context.TProducts
            .Include(p => p.ProductType)
            .Where(p => p.Count.HasValue && p.Count <= threshold && p.Count > 0)
            .OrderBy(p => p.Count)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Count,
                ProductType = p.ProductType != null ? p.ProductType.Name : null,
                p.SellingPrice
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetProductStatistics()
    {
        var totalProducts = await _context.TProducts.CountAsync();

        var totalValue = await _context.TProducts
            .Where(p => p.Count.HasValue && p.PurchasePrice.HasValue && p.Count > 0)
            .SumAsync(p => (decimal)p.Count.Value * p.PurchasePrice.Value);

        var outOfStockCount = await _context.TProducts
            .CountAsync(p => !p.Count.HasValue || p.Count == 0);

        var lowStockCount = await _context.TProducts
            .CountAsync(p => p.Count.HasValue && p.Count <= 5 && p.Count > 0);

        return Ok(new
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = (long)totalValue,
            OutOfStockProducts = outOfStockCount,
            LowStockProducts = lowStockCount
        });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }

        var product = await _context.TProducts
            .Include(p => p.ProductType)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Icon,
                p.PurchasePrice,
                p.SellingPrice,
                p.Count,
                p.ProductTypeId,
                ProductType = p.ProductType != null ? new { p.ProductType.Id, p.ProductType.Name } : null,
            })
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        return Ok(product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto productDto)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }

        if (productDto == null)
        {
            return BadRequest(new { Message = "Product data is required" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProduct = await _context.TProducts.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        if (productDto.ProductTypeId.HasValue)
        {
            var productTypeExists = await _context.TProductTypes.AnyAsync(pt => pt.Id == productDto.ProductTypeId);
            if (!productTypeExists)
            {
                return BadRequest(new { Message = "Invalid ProductTypeId - Product type does not exist" });
            }
        }

        existingProduct.Name = productDto.Name?.Trim();
        existingProduct.Icon = productDto.Icon?.Trim();
        existingProduct.PurchasePrice = productDto.PurchasePrice;
        existingProduct.SellingPrice = productDto.SellingPrice;
        existingProduct.Count = productDto.Count;
        existingProduct.ProductTypeId = productDto.ProductTypeId;

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Product updated successfully" });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProductExistsAsync(id))
            {
                return NotFound(new { Message = $"Product with ID {id} was deleted by another user" });
            }
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CreateProduct([FromBody] ProductDto productDto)
    {
        if (productDto == null)
        {
            return BadRequest(new { Message = "Product data is required" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (productDto.ProductTypeId.HasValue)
        {
            var productTypeExists = await _context.TProductTypes.AnyAsync(pt => pt.Id == productDto.ProductTypeId);
            if (!productTypeExists)
            {
                return BadRequest(new { Message = "Invalid ProductTypeId - Product type does not exist" });
            }
        }

        var product = new TProducts
        {
            Name = productDto.Name?.Trim(),
            Icon = productDto.Icon?.Trim(),
            PurchasePrice = productDto.PurchasePrice,
            SellingPrice = productDto.SellingPrice,
            Count = productDto.Count ?? 0,
            ProductTypeId = productDto.ProductTypeId
        };

        _context.TProducts.Add(product);
        await _context.SaveChangesAsync();

        var result = new
        {
            product.Id,
            product.Name,
            product.Icon,
            product.PurchasePrice,
            product.SellingPrice,
            product.Count,
            product.ProductTypeId
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
    }

    [HttpPost("{id:int}/stock/add")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddStock(int id, [FromBody] ProductStockDto stockDto)
    {
        if (id <= 0)
            return BadRequest(new { Message = "Invalid product ID" });

        if (stockDto == null || !ModelState.IsValid)
            return BadRequest(new { Message = "Stock data is required and must be valid" });

        if (stockDto.Quantity <= 0)
            return BadRequest(new { Message = "Quantity must be greater than zero" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
                return NotFound(new { Message = $"Product with ID {id} not found" });

            var currentStock = product.Count ?? 0;
            if (currentStock > int.MaxValue - stockDto.Quantity)
                return BadRequest(new { Message = "Stock overflow - resulting quantity would be too large" });

            product.Count = currentStock + stockDto.Quantity;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Stock added successfully", NewCount = product.Count });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while adding stock.");
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var product = await _context.TProducts.FindAsync(id);
            if (product == null)
                return NotFound(new { Message = $"Product with ID {id} not found" });

            var currentStock = product.Count ?? 0;
            if (currentStock < stockDto.Quantity)
                return BadRequest(new { Message = $"Insufficient stock. Current stock: {currentStock}, Requested: {stockDto.Quantity}" });

            product.Count = currentStock - stockDto.Quantity;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Stock removed successfully", NewCount = product.Count });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "An error occurred while removing stock.");
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

        var invalidPrices = priceUpdates.Where(p => p.Value < 0).ToList();
        if (invalidPrices.Any())
        {
            return BadRequest(new { Message = "Prices cannot be negative" });
        }

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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { Message = "Invalid product ID" });
        }

        var product = await _context.TProducts
            .Include(p => p.OrderDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        if (product.OrderDetails?.Any() == true)
        {
            return BadRequest(new { Message = "Cannot delete product that has order history. Consider deactivating instead." });
        }

        _context.TProducts.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Product deleted successfully" });
    }

    private async Task<bool> ProductExistsAsync(int id)
    {
        return await _context.TProducts.AnyAsync(e => e.Id == id);
    }
}