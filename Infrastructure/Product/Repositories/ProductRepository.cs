namespace Infrastructure.Product.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly LedkaContext _context;

    public ProductRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.Product.Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdWithVariantsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdWithAllDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .Include(p => p.Reviews.Where(r => !r.IsDeleted))
            .Include(p => p.Images)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdWithVariantsIncludingDeletedAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var query = _context.Products.Where(p => p.Sku == normalizedSku && !p.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsByNameInCategoryAsync(
        string name, int categoryGroupId, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Products
            .Where(p => p.Name == name && p.CategoryGroupId == categoryGroupId && !p.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<Domain.Product.Product> AddAsync(Domain.Product.Product product, CancellationToken ct = default)
    {
        var entry = await _context.Products.AddAsync(product, ct);
        return entry.Entity;
    }

    Task IProductRepository.AddAsync(Domain.Product.Product product, CancellationToken ct)
    {
        return AddAsync(product, ct);
    }

    public void Update(Domain.Product.Product product)
    {
        _context.Products.Update(product);
    }

    public void SetOriginalRowVersion(Domain.Product.Product entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    public void SetVariantOriginalRowVersion(ProductVariant entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(v => v.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippingMethods)
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);
    }

    public async Task<ProductVariant?> GetVariantWithProductAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippingMethods)
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);
    }

    public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.ProductVariantShippingMethods)
            .Include(v => v.VariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    async Task<ProductVariant> IProductRepository.GetVariantByIdForUpdateAsync(int variantId)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippingMethods)
            .Include(v => v.VariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        return variant ?? throw new Domain.Common.Exceptions.DomainException($"واریانت با شناسه {variantId} یافت نشد.");
    }

    public void UpdateVariant(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public void UpdateVariant(object variant)
    {
        if (variant is ProductVariant pv)
        {
            _context.ProductVariants.Update(pv);
        }
        else
        {
            throw new ArgumentException("Object must be of type ProductVariant.", nameof(variant));
        }
    }

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync(ct);
    }
}