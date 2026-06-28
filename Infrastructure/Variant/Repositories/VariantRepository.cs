using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Repositories;

public sealed class VariantRepository(DBContext context) : IVariantRepository
{
    public async Task<ProductVariant?> GetByIdAsync(
        VariantId id,
        CancellationToken ct = default)
        => await context.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetForUpdateAsync(
        VariantId id,
        CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Attributes)
            .Include(v => v.Shippings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetWithProductAsync(
        VariantId id,
        CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<ProductVariant?> GetVariantWithShippingsAsync(
        VariantId id,
        CancellationToken ct = default)
        => await context.ProductVariants
            .Include(v => v.Shippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(
        IEnumerable<VariantId> ids, CancellationToken ct = default)
    {
        var idValues = ids.ToList();
        var result = await context.ProductVariants
            .Where(v => idValues.Contains(v.Id))
            .ToListAsync(ct);
        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductVariant>> GetByIdsWithShippingsAsync(
        IEnumerable<VariantId> ids, CancellationToken ct = default)
    {
        var idValues = ids.Distinct().ToList();
        if (idValues.Count == 0)
            return Array.Empty<ProductVariant>();

        var result = await context.ProductVariants
            .Include(v => v.Shippings)
            .Where(v => idValues.Contains(v.Id))
            .AsSplitQuery()
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    public async Task<bool> ExistsBySkuAsync(
        Sku sku,
        VariantId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Sku == sku && !v.IsDeleted);

        if (excludeId is not null)
            query = query.Where(v => v.Id != excludeId);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        ProductVariant variant,
        CancellationToken ct = default)
    {
        await context.ProductVariants.AddAsync(variant, ct);

        foreach (var attribute in variant.Attributes)
        {
            var entry = context.Entry(attribute);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Added;
        }

        foreach (var shipping in variant.Shippings)
        {
            var entry = context.Entry(shipping);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Added;
        }
    }

    public void Update(ProductVariant variant)
    {
        var entry = context.Entry(variant);
        if (entry.State == EntityState.Detached)
            context.ProductVariants.Attach(variant);
        entry.State = EntityState.Modified;
    }

    public async Task<bool> ExistsAsync(VariantId id, CancellationToken ct = default)
        => await context.ProductVariants
            .AsNoTracking()
            .AnyAsync(v => v.Id == id && !v.IsDeleted, ct);

    public async Task<bool> ExistsByAttributeCombinationAsync(
        ProductId productId,
        IReadOnlyCollection<Guid> attributeValueIdsSorted,
        VariantId? excludeId,
        CancellationToken ct = default)
    {
        if (attributeValueIdsSorted.Count == 0)
            return false;

        var targetCount = attributeValueIdsSorted.Count;
        var targetSet = attributeValueIdsSorted.ToHashSet();

        var query = context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId && !v.IsDeleted);

        if (excludeId is not null)
            query = query.Where(v => v.Id != excludeId);

        var candidates = await query
            .Select(v => new
            {
                VariantId = v.Id,
                ValueIds = v.Attributes.Select(a => a.ValueId).ToList()
            })
            .ToListAsync(ct);

        return candidates.Any(c =>
            c.ValueIds.Count == targetCount &&
            c.ValueIds.All(id => targetSet.Contains(id)));
    }
}