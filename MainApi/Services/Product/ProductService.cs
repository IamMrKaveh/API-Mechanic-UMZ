namespace MainApi.Services.Product;

public class ProductService : BaseApiController, IProductService
{
    private readonly MechanicContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IStorageService _storageService;
    private readonly string _baseUrl;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public ProductService(
        MechanicContext context,
        ILogger<ProductService> logger,
        IStorageService storageService,
        IConfiguration configuration,
        IHtmlSanitizer htmlSanitizer)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
        _baseUrl = configuration["LiaraStorage:BaseUrl"] ?? "https://storage.c2.liara.space/mechanic-umz";
        _htmlSanitizer = htmlSanitizer;
    }

    public async Task<(IEnumerable<PublicProductViewDto> products, int totalItems)> GetProductsAsync(ProductSearchDto search)
    {
        var query = _context.TProducts.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var pattern = $"%{search.Name}%";
            query = query.Where(p => p.Name != null && EF.Functions.ILike(p.Name, pattern));
        }

        if (search.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == search.CategoryId);
        if (search.MinPrice.HasValue)
            query = query.Where(p => p.SellingPrice >= search.MinPrice.Value);
        if (search.MaxPrice.HasValue)
            query = query.Where(p => p.SellingPrice <= search.MaxPrice.Value);
        if (search.InStock == true)
            query = query.Where(p => (!p.IsUnlimited && p.Count > 0) || p.IsUnlimited);
        if (search.HasDiscount == true)
            query = query.Where(p => p.OriginalPrice > p.SellingPrice);
        if (search.IsUnlimited == true)
            query = query.Where(p => p.IsUnlimited);

        query = search.SortBy switch
        {
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.SellingPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.SellingPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) : 0).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountAsc => query.OrderBy(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) : 0).ThenByDescending(p => p.Id),
            ProductSortOptions.Oldest => query.OrderBy(p => p.Id),
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
                Icon = p.Icon,
                Colors = p.Colors,
                Sizes = p.Sizes,
                OriginalPrice = p.OriginalPrice,
                SellingPrice = p.SellingPrice,
                Count = p.Count,
                IsUnlimited = p.IsUnlimited,
                CategoryId = p.CategoryId,
                Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null,
            })
            .ToListAsync();

        items.ForEach(p => p.Icon = ToAbsoluteUrl(p.Icon));
        return (items, totalItems);
    }

    public async Task<object?> GetProductByIdAsync(int id, bool isAdmin)
    {
        var product = await _context.TProducts
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        if (isAdmin)
        {
            return new AdminProductViewDto
            {
                Id = product.Id,
                Name = product.Name,
                Icon = ToAbsoluteUrl(product.Icon),
                Colors = product.Colors,
                Sizes = product.Sizes,
                PurchasePrice = product.PurchasePrice,
                OriginalPrice = product.OriginalPrice,
                SellingPrice = product.SellingPrice,
                Count = product.Count,
                IsUnlimited = product.IsUnlimited,
                CategoryId = product.CategoryId,
                Category = product.Category != null ? new { product.Category.Id, product.Category.Name } : null,
                RowVersion = product.RowVersion
            };
        }

        return new PublicProductViewDto
        {
            Id = product.Id,
            Name = product.Name,
            Icon = ToAbsoluteUrl(product.Icon),
            Colors = product.Colors,
            Sizes = product.Sizes,
            OriginalPrice = product.OriginalPrice,
            SellingPrice = product.SellingPrice,
            Count = product.Count,
            IsUnlimited = product.IsUnlimited,
            CategoryId = product.CategoryId,
            Category = product.Category != null ? new { product.Category.Id, product.Category.Name } : null
        };
    }

    public async Task<TProducts> CreateProductAsync(ProductDto productDto)
    {
        string? iconRelativePath = null;

        var product = new TProducts
        {
            Name = _htmlSanitizer.Sanitize(productDto.Name),
            Colors = productDto.Colors ?? Array.Empty<string>(),
            Sizes = productDto.Sizes ?? Array.Empty<string>(),
            PurchasePrice = productDto.PurchasePrice,
            SellingPrice = productDto.SellingPrice,
            OriginalPrice = productDto.OriginalPrice,
            Count = productDto.IsUnlimited ? 0 : productDto.Count,
            IsUnlimited = productDto.IsUnlimited,
            CategoryId = productDto.CategoryId
        };

        _context.TProducts.Add(product);
        await _context.SaveChangesAsync();

        if (productDto.IconFile != null)
        {
            iconRelativePath = await _storageService.UploadFileAsync(
                productDto.IconFile,
                "images/products",
                product.Id
            );
            product.Icon = iconRelativePath;
            await _context.SaveChangesAsync();
        }

        return product;
    }

    public async Task<bool> UpdateProductAsync(int id, ProductDto productDto)
    {
        var existingProduct = await _context.TProducts.FindAsync(id);
        if (existingProduct == null) return false;

        if (productDto.RowVersion != null)
            _context.Entry(existingProduct).Property("RowVersion").OriginalValue = productDto.RowVersion;

        if (productDto.IconFile != null)
        {
            if (!string.IsNullOrEmpty(existingProduct.Icon))
            {
                await _storageService.DeleteFileAsync(existingProduct.Icon);
            }
            existingProduct.Icon = await _storageService.UploadFileAsync(
                productDto.IconFile,
                "images/products",
                id
            );
        }

        existingProduct.Name = _htmlSanitizer.Sanitize(productDto.Name);
        existingProduct.Colors = productDto.Colors ?? Array.Empty<string>();
        existingProduct.Sizes = productDto.Sizes ?? Array.Empty<string>();
        existingProduct.PurchasePrice = productDto.PurchasePrice;
        existingProduct.SellingPrice = productDto.SellingPrice;
        existingProduct.OriginalPrice = productDto.OriginalPrice;
        existingProduct.Count = productDto.IsUnlimited ? 0 : productDto.Count;
        existingProduct.IsUnlimited = productDto.IsUnlimited;
        existingProduct.CategoryId = productDto.CategoryId;

        _context.Entry(existingProduct).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool success, string? message)> DeleteProductAsync(int id)
    {
        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return (false, $"Product with ID {id} not found");

        var hasOrderHistory = await _context.TOrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrderHistory) return (false, "Cannot delete product that has order history. Consider deactivating instead.");

        string? iconPath = product.Icon;

        _context.TProducts.Remove(product);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(iconPath))
        {
            await _storageService.DeleteFileAsync(iconPath);
        }

        return (true, "Product deleted successfully");
    }

    public async Task<(bool success, int? newCount, string? message)> AddStockAsync(int id, ProductStockDto stockDto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return (false, null, $"Product with ID {id} not found");
        if (product.IsUnlimited) return (false, null, "Cannot change stock for an unlimited product.");

        product.Count += stockDto.Quantity;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, product.Count, "Stock added successfully");
    }

    public async Task<(bool success, int? newCount, string? message)> RemoveStockAsync(int id, ProductStockDto stockDto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return (false, null, $"Product with ID {id} not found");
        if (product.IsUnlimited) return (false, null, "Cannot change stock for an unlimited product.");
        if (product.Count < stockDto.Quantity) return (false, product.Count, $"Insufficient stock. Current stock: {product.Count}, Requested: {stockDto.Quantity}");

        product.Count -= stockDto.Quantity;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, product.Count, "Stock removed successfully");
    }

    public async Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold = 5)
    {
        return await _context.TProducts
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
    }

    public async Task<object> GetProductStatisticsAsync()
    {
        var totalProducts = await _context.TProducts.CountAsync();
        var totalValue = await _context.TProducts
            .Where(p => !p.IsUnlimited && p.Count > 0)
            .SumAsync(p => (decimal)p.Count * p.PurchasePrice);
        var outOfStockCount = await _context.TProducts
            .CountAsync(p => !p.IsUnlimited && p.Count == 0);
        var lowStockCount = await _context.TProducts
            .CountAsync(p => !p.IsUnlimited && p.Count <= 5 && p.Count > 0);

        return new
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = (long)totalValue,
            OutOfStockProducts = outOfStockCount,
            LowStockProducts = lowStockCount
        };
    }

    public async Task<(int updatedCount, string? message)> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice)
    {
        var productIds = priceUpdates.Keys.ToList();
        var products = await _context.TProducts
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (!products.Any()) return (0, "No products found with the provided IDs");

        var updatedCount = 0;
        foreach (var product in products)
        {
            if (priceUpdates.TryGetValue(product.Id, out var newPrice))
            {
                if (isPurchasePrice)
                    product.PurchasePrice = newPrice;
                else
                    product.SellingPrice = newPrice;
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync();
        return (updatedCount, $"{updatedCount} products updated successfully");
    }

    public async Task<(IEnumerable<object> products, int totalItems)> GetDiscountedProductsAsync(int page, int pageSize, int minDiscount, int maxDiscount, int categoryId)
    {
        var query = _context.TProducts
            .Include(p => p.Category)
            .Where(p => p.OriginalPrice > p.SellingPrice && (p.Count > 0 || p.IsUnlimited))
            .AsQueryable();

        if (categoryId > 0)
            query = query.Where(p => p.CategoryId == categoryId);
        if (minDiscount > 0)
            query = query.Where(p => p.OriginalPrice > 0 && ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) >= minDiscount);
        if (maxDiscount > 0)
            query = query.Where(p => p.OriginalPrice > 0 && ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) <= maxDiscount);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) : 0)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Icon = ToAbsoluteUrl(p.Icon),
                p.Colors,
                p.Sizes,
                p.OriginalPrice,
                p.SellingPrice,
                DiscountAmount = p.OriginalPrice - p.SellingPrice,
                DiscountPercentage = p.OriginalPrice > 0 ? ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice) : 0,
                p.Count,
                p.IsUnlimited,
                p.CategoryId,
                Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null
            })
            .ToListAsync();

        return (items, totalItems);
    }

    public async Task<(bool success, object? result, string? message)> SetProductDiscountAsync(int id, SetDiscountDto discountDto)
    {
        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return (false, null, $"Product with ID {id} not found");

        product.OriginalPrice = discountDto.OriginalPrice;
        product.SellingPrice = discountDto.DiscountedPrice;

        await _context.SaveChangesAsync();

        var discountPercentage = ((double)(discountDto.OriginalPrice - discountDto.DiscountedPrice) * 100.0 / (double)discountDto.OriginalPrice);
        var result = new
        {
            Message = "Discount applied successfully",
            DiscountPercentage = Math.Round((decimal)discountPercentage, 2),
            OriginalPrice = product.OriginalPrice,
            DiscountedPrice = product.SellingPrice
        };

        return (true, result, "Discount applied successfully");
    }

    public async Task<(bool success, string? message)> RemoveProductDiscountAsync(int id)
    {
        var product = await _context.TProducts.FindAsync(id);
        if (product == null) return (false, $"Product with ID {id} not found");
        if (product.OriginalPrice <= product.SellingPrice) return (false, "Product does not have a valid discount to remove");

        product.SellingPrice = product.OriginalPrice;
        product.OriginalPrice = 0;

        await _context.SaveChangesAsync();

        return (true, "Discount removed successfully");
    }

    public async Task<object> GetDiscountStatisticsAsync()
    {
        var totalDiscountedProducts = await _context.TProducts
            .CountAsync(p => p.OriginalPrice > p.SellingPrice);

        var averageDiscountPercentage = await _context.TProducts
            .Where(p => p.OriginalPrice > 0 && p.OriginalPrice > p.SellingPrice)
            .Select(p => ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice))
            .DefaultIfEmpty(0)
            .AverageAsync();

        var totalDiscountValue = await _context.TProducts
            .Where(p => p.OriginalPrice > p.SellingPrice && !p.IsUnlimited && p.Count > 0)
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
                AverageDiscount = g.Average(p => ((double)(p.OriginalPrice - p.SellingPrice) * 100.0 / (double)p.OriginalPrice))
            })
            .ToListAsync();

        return new
        {
            TotalDiscountedProducts = totalDiscountedProducts,
            AverageDiscountPercentage = Math.Round(averageDiscountPercentage, 2),
            TotalDiscountValue = totalDiscountValue,
            DiscountByCategory = discountByCategory
        };
    }
}