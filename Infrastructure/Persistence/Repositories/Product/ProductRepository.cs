namespace Infrastructure.Persistence.Repositories.Product;

public class ProductRepository : IProductRepository
{
    private readonly LedkaContext _context;

    public ProductRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(List<Domain.Product.Product> Products, int TotalItems)> GetPagedAsync(ProductSearchDto searchParams)
    {
        var query = _context.Products
            .Include(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .Include(p => p.Variants)
            .ThenInclude(v => v.Images)
            .Include(p => p.Variants)
            .ThenInclude(v => v.VariantAttributes)
            .ThenInclude(va => va.AttributeValue)
            .ThenInclude(av => av.AttributeType)
            .AsSplitQuery()
            .AsQueryable();

        // 1. Filter: Active/Deleted
        if (searchParams.IncludeDeleted == true)
        {
            query = query.IgnoreQueryFilters();
        }

        if (searchParams.IncludeInactive != true)
        {
            query = query.Where(p => p.IsActive);
        }

        // 2. Filter: Search (Name, Description, Category, Group)
        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            var term = PersianTextHelper.Normalize(searchParams.Name);
            var pattern = $"%{term}%";

            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                EF.Functions.ILike(p.Description ?? "", pattern) ||
                EF.Functions.ILike(p.CategoryGroup.Name, pattern) ||
                EF.Functions.ILike(p.CategoryGroup.Category.Name, pattern)
            );
        }

        // 3. Filter: Category
        if (searchParams.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryGroup.CategoryId == searchParams.CategoryId.Value);
        }

        // 4. Filter: Category Group
        if (searchParams.CategoryGroupId.HasValue)
        {
            query = query.Where(p => p.CategoryGroupId == searchParams.CategoryGroupId.Value);
        }

        // 5. Filter: Price Range
        if (searchParams.MinPrice.HasValue)
        {
            query = query.Where(p => p.MaxPrice >= searchParams.MinPrice.Value);
        }

        if (searchParams.MaxPrice.HasValue)
        {
            query = query.Where(p => p.MinPrice <= searchParams.MaxPrice.Value);
        }

        // 6. Filter: In Stock
        if (searchParams.InStock == true)
        {
            query = query.Where(p => p.TotalStock > 0 || p.Variants.Any(v => v.IsUnlimited && v.IsActive));
        }

        // 7. Filter: Discount
        if (searchParams.HasDiscount == true)
        {
            query = query.Where(p => p.Variants.Any(v => v.IsActive && v.OriginalPrice > v.SellingPrice));
        }

        // 8. Filter: Unlimited
        if (searchParams.IsUnlimited == true)
        {
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited && v.IsActive));
        }

        var totalItems = await query.CountAsync();

        // 9. Sort
        query = searchParams.SortBy switch
        {
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.MinPrice),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.MinPrice),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name),
            ProductSortOptions.Newest => query.OrderByDescending(p => p.CreatedAt),
            ProductSortOptions.Oldest => query.OrderBy(p => p.CreatedAt),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p =>
                p.Variants.Any() ?
                p.Variants.Max(v => v.OriginalPrice > 0 ? (v.OriginalPrice - v.SellingPrice) / v.OriginalPrice : 0) : 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // 10. Paginate
        var products = await query
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync();

        return (products, totalItems);
    }

    public async Task<Domain.Product.Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeAll = false)
    {
        var query = _context.Products
            .Include(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .Include(p => p.Variants)
            .ThenInclude(v => v.Images)
            .Include(p => p.Variants)
            .ThenInclude(v => v.VariantAttributes)
            .ThenInclude(va => va.AttributeValue)
            .ThenInclude(av => av.AttributeType)
            .AsSplitQuery();

        if (includeAll)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<Domain.Product.Product?> GetByIdWithCategoryAndMediaAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids)
    {
        return await _context.AttributeValues
            .Include(av => av.AttributeType)
            .Where(av => ids.Contains(av.Id))
            .ToListAsync();
    }

    public async Task<List<AttributeType>> GetAllAttributeTypesWithValuesAsync()
    {
        return await _context.AttributeTypes
            .Include(at => at.AttributeValues)
            .OrderBy(at => at.SortOrder)
            .ToListAsync();
    }

    public async Task AddAsync(Domain.Product.Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public void UpdateVariants(Domain.Product.Product product, List<CreateProductVariantDto> variantDtos)
    {
        // Implementation handled by service logic updating collection
    }

    public void SetOriginalRowVersion(Domain.Product.Product product, byte[] rowVersion)
    {
        _context.Entry(product).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }

    public void Update(Domain.Product.Product product)
    {
        _context.Products.Update(product);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(int variantId)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public void SetVariantRowVersion(ProductVariant variant, byte[] rowVersion)
    {
        _context.Entry(variant).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }

    public void UpdateVariant(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public async Task<Dictionary<int, ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id);
    }

    public async Task<bool> SkuExistsAsync(string sku, int? variantId = null)
    {
        var query = _context.ProductVariants.AsQueryable();
        if (variantId.HasValue)
        {
            query = query.Where(v => v.Id != variantId.Value);
        }
        return await query.AnyAsync(v => v.Sku == sku);
    }

    public async Task<bool> ProductSkuExistsAsync(string sku, int? productId = null)
    {
        var query = _context.Products.AsQueryable();
        if (productId.HasValue)
        {
            query = query.Where(p => p.Id != productId.Value);
        }
        return await query.AnyAsync(p => p.Sku == sku);
    }

    public async Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold)
    {
        return await _context.ProductVariants
            .Where(v => !v.IsUnlimited && v.StockQuantity <= threshold && v.IsActive)
            .Select(v => new
            {
                v.Id,
                ProductName = v.Product.Name,
                v.Sku,
                Stock = v.StockQuantity
            })
            .ToListAsync();
    }

    public async Task<object> GetProductStatisticsAsync()
    {
        var total = await _context.Products.CountAsync();
        var active = await _context.Products.CountAsync(p => p.IsActive);
        var outOfStock = await _context.Products.CountAsync(p => p.TotalStock == 0);

        return new { Total = total, Active = active, OutOfStock = outOfStock };
    }
}