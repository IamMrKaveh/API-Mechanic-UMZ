namespace Infrastructure.Variant.Repositories;

public class VariantRepository : IVariantRepository
{
    private readonly Persistence.Context.DBContext _context;

    public VariantRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<ProductVariant?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
    }

    public async Task<ProductVariant?> GetByIdForUpdateAsync(int id, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.VariantAttributes)
            .Include(v => v.ProductVariantShippings)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<ProductVariant?> GetWithProductAsync(int id, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .Where(v => ids.Contains(v.Id) && !v.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
    {
        await _context.ProductVariants.AddAsync(variant, ct);
    }

    public void Update(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public void SetOriginalRowVersion(ProductVariant entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(v => v.RowVersion).OriginalValue = rowVersion;
    }
}