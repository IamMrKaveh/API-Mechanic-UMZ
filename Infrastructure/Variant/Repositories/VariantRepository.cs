using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Repositories;

public sealed class VariantRepository(DBContext context) : IVariantRepository
{
    public async Task<ProductVariant?> GetByIdAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetByIdForUpdateAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetWithProductAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetVariantWithShippingsAsync(
        VariantId id, CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.ProductVariantShippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetBySkuAsync(Sku sku, CancellationToken ct = default)
        => await context.ProductVariants
            .FirstOrDefaultAsync(v => v.Sku.Value == sku.Value, ct);

    public async Task<IReadOnlyList<ProductVariant>> GetActiveByProductIdAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var result = await context.ProductVariants
            .Where(v => v.ProductId == productId && v.IsActive && !v.IsDeleted)
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(
        IEnumerable<VariantId> ids, CancellationToken ct = default)
    {
        var idValues = ids.Select(id => id.Value).ToList();
        var result = await context.ProductVariants
            .Where(v => idValues.Contains(v.Id.Value))
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants.AnyAsync(v => v.Id == id, ct);

    public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
        => await context.ProductVariants.AddAsync(variant, ct);

    public void Update(ProductVariant variant)
        => context.ProductVariants.Update(variant);
}