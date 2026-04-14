using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Variant.Repositories;

public sealed class VariantRepository(DBContext context) : IVariantRepository
{
    public async Task<ProductVariant?> GetByIdAsync(VariantId variantId, CancellationToken ct = default)
    {
        return await context.ProductVariants
            .Include(v => v.Attributes)
            .ThenInclude(va => va.AttributeValue)
            .ThenInclude(av => av!.AttributeType)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(
        Domain.Product.ValueObjects.ProductId productId,
        CancellationToken ct = default)
    {
        var results = await context.ProductVariants
            .Include(v => v.Attributes)
            .ThenInclude(va => va.AttributeValue)
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<bool> ExistsBySkuAsync(Sku sku, VariantId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.ProductVariants.Where(v => v.Sku == sku.Value && !v.IsDeleted);
        if (excludeId is not null)
            query = query.Where(v => v.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(ProductVariant variant, CancellationToken ct = default)
    {
        await context.ProductVariants.AddAsync(variant, ct);
    }

    public void Update(ProductVariant variant)
    {
        context.ProductVariants.Update(variant);
    }
}