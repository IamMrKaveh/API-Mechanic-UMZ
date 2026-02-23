namespace Infrastructure.Product.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly Persistence.Context.DBContext _context;

    public ProductRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<Domain.Product.Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdWithAllDetailsAsync(int id, CancellationToken ct = default)
    {
        // Optimized to use SplitQuery
        return await _context.Products
            .AsSplitQuery()
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdWithVariantsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
           .Include(p => p.Variants)
           .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Domain.Product.Product?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default)
    {
        return await _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeProductId = null, CancellationToken ct = default)
    {
        var normalizedSku = sku.Trim().ToUpperInvariant();
        var query = _context.ProductVariants
            .Where(v => v.Sku.Value == normalizedSku && !v.IsDeleted);

        if (excludeProductId.HasValue)
        {
            query = query.Where(v => v.ProductId != excludeProductId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.Product.Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
    }

    public void Update(Domain.Product.Product product)
    {
        _context.Products.Update(product);
    }

    public void SetOriginalRowVersion(Domain.Product.Product entity, byte[] rowVersion)
    {
        _context.Entry(entity)
            .Property(p => p.RowVersion)
            .OriginalValue = rowVersion;
    }

    public Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}