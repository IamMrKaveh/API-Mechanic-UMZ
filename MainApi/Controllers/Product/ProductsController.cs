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
    public async Task<ActionResult<IEnumerable<object>>> GetTProducts([FromQuery] ProductSearchDto search)
    {
        if (search == null)
        {
            search = new ProductSearchDto();
        }

        if (search.Page < 1) search.Page = 1;
        if (search.PageSize < 1) search.PageSize = 10;
        if (search.PageSize > 100) search.PageSize = 100;

        var query = _context.TProducts.Include(p => p.ProductType).AsQueryable();

        if (!string.IsNullOrEmpty(search.Name))
        {
            query = query.Where(p => p.Name.Contains(search.Name));
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
    public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts([FromQuery] int threshold = 5)
    {
        if (threshold < 0) threshold = 5;

        var products = await _context.TProducts
            .Include(p => p.ProductType)
            .Where(p => p.Count.HasValue && p.Count <= threshold)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Count,
                ProductType = p.ProductType != null ? p.ProductType.Name : null,
                p.SellingPrice
            })
            .OrderBy(p => p.Count)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetProductStatistics()
    {
        var totalProducts = await _context.TProducts.CountAsync();
        var totalValue = await _context.TProducts
            .Where(p => p.Count.HasValue && p.PurchasePrice.HasValue)
            .SumAsync(p => (long?)p.Count * p.PurchasePrice);
        var outOfStockCount = await _context.TProducts
            .CountAsync(p => !p.Count.HasValue || p.Count == 0);
        var lowStockCount = await _context.TProducts
            .CountAsync(p => p.Count.HasValue && p.Count <= 5 && p.Count > 0);

        return Ok(new
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = totalValue ?? 0,
            OutOfStockProducts = outOfStockCount,
            LowStockProducts = lowStockCount
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTProducts(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID");
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
            return NotFound($"Product with ID {id} not found");
        }

        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTProducts(int id, ProductDto productDto)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID");
        }

        if (productDto == null)
        {
            return BadRequest("Product data is required");
        }

        if (id != productDto.Id)
        {
            return BadRequest("ID mismatch between URL and request body");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProduct = await _context.TProducts.FindAsync(id);
        if (existingProduct == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        if (productDto.ProductTypeId.HasValue)
        {
            var productTypeExists = await _context.TProductTypes.AnyAsync(pt => pt.Id == productDto.ProductTypeId);
            if (!productTypeExists)
            {
                return BadRequest("Invalid ProductTypeId - Product type does not exist");
            }
        }

        existingProduct.Name = productDto.Name;
        existingProduct.Icon = productDto.Icon;
        existingProduct.PurchasePrice = productDto.PurchasePrice;
        existingProduct.SellingPrice = productDto.SellingPrice;
        existingProduct.Count = productDto.Count;
        existingProduct.ProductTypeId = productDto.ProductTypeId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TProductsExists(id))
            {
                return NotFound($"Product with ID {id} was deleted by another user");
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<TProducts>> PostTProducts(ProductDto productDto)
    {
        if (productDto == null)
        {
            return BadRequest("Product data is required");
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
                return BadRequest("Invalid ProductTypeId - Product type does not exist");
            }
        }

        var product = new TProducts
        {
            Name = productDto.Name,
            Icon = productDto.Icon,
            PurchasePrice = productDto.PurchasePrice,
            SellingPrice = productDto.SellingPrice,
            Count = productDto.Count ?? 0,
            ProductTypeId = productDto.ProductTypeId
        };

        _context.TProducts.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTProducts), new { id = product.Id }, product);
    }

    [HttpPost("{id}/stock/add")]
    public async Task<IActionResult> AddStock(int id, ProductStockDto stockDto)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID");
        }

        if (stockDto == null)
        {
            return BadRequest("Stock data is required");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (stockDto.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero");
        }

        var product = await _context.TProducts.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        var currentStock = product.Count ?? 0;
        if (currentStock > int.MaxValue - stockDto.Quantity)
        {
            return BadRequest("Stock overflow - resulting quantity would be too large");
        }

        product.Count = currentStock + stockDto.Quantity;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Stock added successfully", NewCount = product.Count });
    }

    [HttpPost("{id}/stock/remove")]
    public async Task<IActionResult> RemoveStock(int id, ProductStockDto stockDto)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID");
        }

        if (stockDto == null)
        {
            return BadRequest("Stock data is required");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (stockDto.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero");
        }

        var product = await _context.TProducts.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        var currentStock = product.Count ?? 0;
        if (currentStock < stockDto.Quantity)
        {
            return BadRequest($"Insufficient stock. Current stock: {currentStock}, Requested: {stockDto.Quantity}");
        }

        product.Count = currentStock - stockDto.Quantity;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Stock removed successfully", NewCount = product.Count });
    }

    [HttpPost("bulk-update-prices")]
    public async Task<IActionResult> BulkUpdatePrices([FromBody] Dictionary<int, int> priceUpdates, [FromQuery] bool isPurchasePrice = false)
    {
        if (priceUpdates == null || !priceUpdates.Any())
        {
            return BadRequest("Price updates data is required");
        }

        var invalidPrices = priceUpdates.Where(p => p.Value < 0).ToList();
        if (invalidPrices.Any())
        {
            return BadRequest("Prices cannot be negative");
        }

        var productIds = priceUpdates.Keys.ToList();
        var products = await _context.TProducts
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (!products.Any())
        {
            return NotFound("No products found with the provided IDs");
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTProducts(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product ID");
        }

        var product = await _context.TProducts
            .Include(p => p.OrderDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        if (product.OrderDetails?.Any() == true)
        {
            return BadRequest("Cannot delete product that has order history. Consider deactivating instead.");
        }

        _context.TProducts.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TProductsExists(int id)
    {
        return _context.TProducts.Any(e => e.Id == id);
    }
}