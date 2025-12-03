namespace Infrastructure.Persistence.Repositories;

public class ProductVariantRepository : IProductVariantRepository
{
    private readonly LedkaContext _context;

    public ProductVariantRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<ProductVariant?> GetByIdAsync(int id, bool includeProduct = false)
    {
        var query = _context.ProductVariants.AsQueryable();

        if (includeProduct)
        {
            query = query.Include(v => v.Product);
        }

        return await query
            .Include(v => v.InventoryTransactions)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<ProductVariant?> GetByIdForUpdateAsync(int id)
    {
        return await _context.ProductVariants
            .FromSqlInterpolated($"SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {id} FOR UPDATE")
            .Include(v => v.Product)
            .Include(v => v.InventoryTransactions)
            .FirstOrDefaultAsync();
    }

    public async Task<ProductVariant?> GetBySkuAsync(string sku)
    {
        return await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Sku == sku);
    }

    public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId)
    {
        return await _context.ProductVariants
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .Include(v => v.InventoryTransactions)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync(int threshold)
    {
        var variants = await _context.ProductVariants
            .Where(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited)
            .Include(v => v.Product)
            .Include(v => v.InventoryTransactions)
            .ToListAsync();

        return variants.Where(v => v.Stock > 0 && v.Stock <= threshold);
    }

    public async Task<IEnumerable<ProductVariant>> GetOutOfStockVariantsAsync()
    {
        var variants = await _context.ProductVariants
            .Where(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited)
            .Include(v => v.Product)
            .Include(v => v.InventoryTransactions)
            .ToListAsync();

        return variants.Where(v => v.Stock == 0);
    }

    public void Update(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
    }

    public void SetOriginalRowVersion(ProductVariant variant, byte[] rowVersion)
    {
        _context.Entry(variant).Property(v => v.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.ProductVariants.AnyAsync(v => v.Id == id && !v.IsDeleted);
    }
}