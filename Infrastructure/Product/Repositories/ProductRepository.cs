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

    public void Update(Domain.Product.Product product)
    {
        _context.Products.Update(product);
    }

    public void SetOriginalRowVersion(Domain.Product.Product entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task<ProductVariant?> GetVariantByIdForUpdateAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.ProductVariantShippingMethods)
            .Include(v => v.VariantAttributes)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public void UpdateVariant(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync(ct);
    }

    public Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Product.Product?> GetByIdWithAllDetailsAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Domain.Product.Product?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    Task IProductRepository.AddAsync(Domain.Product.Product product, CancellationToken ct)
    {
        return AddAsync(product, ct);
    }

    public Task<ProductVariant?> GetVariantWithProductAsync(int variantId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void SetVariantOriginalRowVersion(ProductVariant entity, byte[] rowVersion)
    {
        throw new NotImplementedException();
    }

    public void UpdateVariant(object variant)
    {
        throw new NotImplementedException();
    }

    public Task<ProductVariantResponseDto> GetVariantByIdForUpdateAsync(int variantId)
    {
        throw new NotImplementedException();
    }

    Task<ProductVariant> IProductRepository.GetVariantByIdForUpdateAsync(int variantId)
    {
        throw new NotImplementedException();
    }
}