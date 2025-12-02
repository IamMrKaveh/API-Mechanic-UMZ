namespace Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly LedkaContext _context;
    private readonly IMapper _mapper;

    public ProductRepository(LedkaContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<(List<Product> Products, int TotalItems)> GetPagedAsync(ProductSearchDto searchParams)
    {
        var query = _context.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.InventoryTransactions)
                .Include(p => p.CategoryGroup)
                    .ThenInclude(cg => cg.Category)
                .AsQueryable();

        if (!searchParams.IncludeDeleted == true)
        {
            query = query.Where(p => !p.IsDeleted);
        }

        if (!searchParams.IncludeInactive == true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrEmpty(searchParams.Name))
        {
            query = query.Where(p => EF.Functions.Like(p.Name, $"{searchParams.Name}%"));
        }

        if (searchParams.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryGroup.CategoryId == searchParams.CategoryId.Value);
        }

        if (searchParams.CategoryGroupId.HasValue)
        {
            query = query.Where(p => p.CategoryGroupId == searchParams.CategoryGroupId.Value);
        }

        if (searchParams.MinPrice.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice >= searchParams.MinPrice.Value));
        }

        if (searchParams.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice <= searchParams.MaxPrice.Value));
        }

        if (searchParams.InStock.HasValue && searchParams.InStock.Value)
        {
            query = query.Where(p => p.Variants.Any(v => v.Stock > 0 || v.IsUnlimited));
        }

        if (searchParams.HasDiscount.HasValue && searchParams.HasDiscount.Value)
        {
            query = query.Where(p => p.Variants.Any(v => v.OriginalPrice > v.SellingPrice));
        }

        if (searchParams.IsUnlimited.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited == searchParams.IsUnlimited.Value));
        }

        query = searchParams.SortBy switch
        {
            ProductSortOptions.Newest => query.OrderByDescending(p => p.CreatedAt),
            ProductSortOptions.Oldest => query.OrderBy(p => p.CreatedAt),
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.MinPrice),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.MaxPrice),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p => p.Variants.Any() ? p.Variants.Max(v => (v.OriginalPrice - v.SellingPrice) / v.OriginalPrice) : 0),
            ProductSortOptions.DiscountAsc => query.OrderBy(p => p.Variants.Any() ? p.Variants.Min(v => (v.OriginalPrice - v.SellingPrice) / v.OriginalPrice) : 0),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var products = await query.Skip((searchParams.Page - 1) * searchParams.PageSize).Take(searchParams.PageSize).ToListAsync();

        return (products, totalItems);
    }

    public async Task<Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeAll = false)
    {
        var query = _context.Products.AsQueryable();

        if (includeAll)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .Include(p => p.Variants.Where(v => includeAll || !v.IsDeleted))
                .ThenInclude(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.InventoryTransactions)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<Product?> GetByIdWithCategoryAndMediaAsync(int productId)
    {
        return await _context.Products
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids)
    {
        return await _context.AttributeValues.Where(av => ids.Contains(av.Id)).ToListAsync();
    }

    public async Task<List<AttributeType>> GetAllAttributeTypesWithValuesAsync()
    {
        return await _context.AttributeTypes.Include(at => at.AttributeValues).ToListAsync();
    }


    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public void UpdateVariants(Product product, List<CreateProductVariantDto> variantDtos)
    {
        var existingVariantIds = product.Variants.Select(v => v.Id).ToList();
        var updatedVariantIds = variantDtos.Where(v => v.Id > 0).Select(v => v.Id!.Value).ToList();

        var variantsToDelete = product.Variants.Where(v => !updatedVariantIds.Contains(v.Id)).ToList();
        foreach (var variant in variantsToDelete)
        {
            variant.IsDeleted = true;
            variant.DeletedAt = DateTime.UtcNow;
        }

        foreach (var dto in variantDtos)
        {
            ProductVariant variant;
            if (dto.Id > 0)
            {
                variant = product.Variants.FirstOrDefault(v => v.Id == dto.Id);
                if (variant == null) continue;
                _mapper.Map(dto, variant);
            }
            else
            {
                variant = _mapper.Map<ProductVariant>(dto);
                if (dto.Stock > 0)
                {
                    variant.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "StockIn",
                        QuantityChange = dto.Stock,
                        StockBefore = 0,
                        Notes = "Initial stock (Update)",
                        UserId = null
                    });
                }
                product.Variants.Add(variant);
            }

            variant.VariantAttributes.Clear();
            var attributeValues = _context.AttributeValues.Where(av => dto.AttributeValueIds.Contains(av.Id)).ToList();
            foreach (var attrValue in attributeValues)
            {
                variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValue = attrValue });
            }
        }
    }

    public void SetOriginalRowVersion(Product product, byte[] rowVersion)
    {
        _context.Entry(product).Property("RowVersion").OriginalValue = rowVersion;
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.ProductVariants.FindAsync(variantId);
    }

    public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(int variantId)
    {
        return await _context.ProductVariants
                    .Include(v => v.Product)
                    .Include(v => v.InventoryTransactions)
                    .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public void SetVariantRowVersion(ProductVariant variant, byte[] rowVersion)
    {
        _context.Entry(variant).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    public void UpdateVariant(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public async Task<Dictionary<int, ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds)
    {
        return await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id);
    }

    public async Task<bool> SkuExistsAsync(string sku, int? variantId = null)
    {
        if (string.IsNullOrEmpty(sku)) return false;

        return await _context.ProductVariants
            .AnyAsync(v => v.Sku == sku && (!variantId.HasValue || v.Id != variantId.Value));
    }

    public async Task<bool> ProductSkuExistsAsync(string sku, int? productId = null)
    {
        if (string.IsNullOrEmpty(sku)) return false;

        return await _context.Products
            .AnyAsync(p => p.Sku == sku && (!productId.HasValue || p.Id != productId.Value));
    }

    public async Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold)
    {
        return await _context.ProductVariants
            .Where(v => !v.IsUnlimited && v.Stock > 0 && v.Stock <= threshold)
            .Select(v => new
            {
                v.Product.Id,
                v.Product.Name,
                VariantId = v.Id,
                VariantDisplayName = string.Join(" / ", v.VariantAttributes.Select(va => va.AttributeValue.Value)),
                v.Stock,
                Category = v.Product.CategoryGroup.Category.Name,
                v.SellingPrice
            })
            .ToListAsync();
    }

    public async Task<object> GetProductStatisticsAsync()
    {
        var totalProducts = await _context.Products.CountAsync();
        var totalVariants = await _context.ProductVariants.CountAsync();
        var outOfStockProducts = await _context.ProductVariants.Where(v => !v.IsUnlimited && v.Stock == 0).CountAsync();
        var lowStockProducts = await _context.ProductVariants.Where(v => !v.IsUnlimited && v.Stock > 0 && v.Stock <= 5).CountAsync();
        var totalInventoryValue = await _context.ProductVariants.SumAsync(v => v.PurchasePrice * v.Stock);

        return new
        {
            TotalProducts = totalProducts,
            TotalVariants = totalVariants,
            OutOfStockProducts = outOfStockProducts,
            LowStockProducts = lowStockProducts,
            TotalInventoryValue = totalInventoryValue
        };
    }
}