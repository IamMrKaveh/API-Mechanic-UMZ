using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Repositories;

public sealed class VariantRepository(DBContext context) : IVariantRepository
{
    public async Task<ProductVariant?> GetByIdAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetWithProductAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetVariantWithShippingsAsync(
        VariantId id, CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Shippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(
        IEnumerable<VariantId> ids, CancellationToken ct = default)
    {
        var idValues = ids.Select(id => id.Value).ToList();
        var result = await context.ProductVariants
            .Where(v => idValues.Contains(v.Id.Value))
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<bool> ExistsBySkuAsync(
        Sku sku, VariantId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.ProductVariants
            .Where(v => v.Sku.Value == sku.Value && !v.IsDeleted);

        if (excludeId is not null)
            query = query.Where(v => v.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
        => await context.ProductVariants.AddAsync(variant, ct);

    public void Update(ProductVariant variant)
        => context.ProductVariants.Update(variant);
}