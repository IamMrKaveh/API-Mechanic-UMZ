namespace Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly LedkaContext _context;

    public ProductRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(List<Product> products, int totalCount)> GetPagedAsync(ProductSearchDto searchDto)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .AsQueryable();

        if (searchDto.IncludeInactive == true)
        {
            query = _context.Products
               .AsNoTracking()
               .Include(p => p.Variants)
               .Include(p => p.CategoryGroup)
               .ThenInclude(cg => cg.Category)
               .AsQueryable();
        }

        else
        {
            query = query.Where(p => p.IsActive);
        }

        if (!searchDto.IncludeDeleted == true)
        {
            query = query.Where(p => !p.IsDeleted);
        }

        if (searchDto.CategoryGroupId.HasValue)
        {
            query = query.Where(p => p.CategoryGroupId == searchDto.CategoryGroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Name))
        {
            var searchTerm = searchDto.Name.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{searchTerm}%") || (p.Sku != null && EF.Functions.ILike(p.Sku, $"%{searchTerm}%")));
        }

        if (searchDto.CategoryId.HasValue && searchDto.CategoryId > 0)
        {
            query = query.Where(p => p.CategoryGroup.CategoryId == searchDto.CategoryId);
        }

        if (searchDto.MinPrice.HasValue)
        {
            query = query.Where(p => p.MaxPrice >= searchDto.MinPrice.Value);
        }

        if (searchDto.MaxPrice.HasValue)
        {
            query = query.Where(p => p.MinPrice <= searchDto.MaxPrice.Value);
        }

        if (searchDto.InStock.HasValue && searchDto.InStock.Value)
        {
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0));
        }

        if (searchDto.HasDiscount.HasValue && searchDto.HasDiscount.Value)
        {
            query = query.Where(p => p.Variants.Any(v => v.OriginalPrice > v.SellingPrice));
        }

        query = searchDto.SortBy switch
        {
            ProductSortOptions.Newest => query.OrderByDescending(p => p.CreatedAt),
            ProductSortOptions.Oldest => query.OrderBy(p => p.CreatedAt),
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.MinPrice),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.MinPrice),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p => p.Variants.Max(v => (v.OriginalPrice - v.SellingPrice) / v.OriginalPrice)),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };

        var totalCount = await query.CountAsync();
        var products = await query.Skip((searchDto.Page - 1) * searchDto.PageSize).Take(searchDto.PageSize).ToListAsync();

        return (products, totalCount);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task<List<AttributeType>> GetAllAttributesAsync()
    {
        return await _context.AttributeTypes
            .Include(at => at.AttributeValues)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeInactive = false)
    {
        var query = _context.Products.AsQueryable();
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .Include(p => p.Variants)
            .ThenInclude(v => v.VariantAttributes)
            .ThenInclude(va => va.AttributeValue)
            .ThenInclude(av => av.AttributeType)
            .Include(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.ProductVariants.FindAsync(variantId);
    }

    public void UpdateVariants(Product product, List<CreateProductVariantDto> variantDtos)
    {
        var existingVariantIds = product.Variants.Select(v => v.Id).ToList();
        var dtoVariantIds = variantDtos.Where(dto => dto.Id.HasValue).Select(dto => dto.Id!.Value).ToList();

        var variantsToRemove = product.Variants.Where(v => !dtoVariantIds.Contains(v.Id)).ToList();
        foreach (var variant in variantsToRemove)
        {
            _context.ProductVariants.Remove(variant);
        }

        foreach (var variantDto in variantDtos)
        {
            ProductVariant variant;
            if (variantDto.Id.HasValue && existingVariantIds.Contains(variantDto.Id.Value))
            {
                variant = product.Variants.First(v => v.Id == variantDto.Id.Value);
            }
            else
            {
                variant = new ProductVariant { Product = product };
                product.Variants.Add(variant);
            }

            variant.Sku = variantDto.Sku;
            variant.PurchasePrice = variantDto.PurchasePrice;
            variant.SellingPrice = variantDto.SellingPrice;
            variant.OriginalPrice = variantDto.OriginalPrice;
            variant.IsUnlimited = variantDto.IsUnlimited;
            variant.IsActive = variantDto.IsActive;

            var currentAttributeValueIds = variant.VariantAttributes.Select(va => va.AttributeValueId).ToList();
            var newAttributeValueIds = variantDto.AttributeValueIds;

            var toRemove = variant.VariantAttributes.Where(va => !newAttributeValueIds.Contains(va.AttributeValueId)).ToList();
            foreach (var va in toRemove)
            {
                _context.ProductVariantAttributes.Remove(va);
            }

            var toAddIds = newAttributeValueIds.Except(currentAttributeValueIds).ToList();
            if (toAddIds.Any())
            {
                foreach (var attrId in toAddIds)
                {
                    variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValueId = attrId });
                }
            }
        }
    }

    public void SetOriginalRowVersion(Product product, byte[] rowVersion)
    {
        _context.Entry(product).Property("RowVersion").OriginalValue = rowVersion;
    }

    public async Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids)
    {
        return await _context.AttributeValues.Where(av => ids.Contains(av.Id)).ToListAsync();
    }
}