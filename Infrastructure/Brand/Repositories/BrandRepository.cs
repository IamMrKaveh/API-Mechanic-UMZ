using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Brand.Repositories;

public sealed class BrandRepository(DBContext context) : IBrandRepository
{
    public async Task<Domain.Brand.Aggregates.Brand?> GetByIdAsync(BrandId brandId, CancellationToken ct = default)
    {
        return await context.Brands.FirstOrDefaultAsync(b => b.Id == brandId, ct);
    }

    public async Task<Domain.Brand.Aggregates.Brand?> GetBySlugAsync(Slug slug, CancellationToken ct = default)
    {
        return await context.Brands.FirstOrDefaultAsync(b => b.Slug == slug.Value, ct);
    }

    public async Task<bool> ExistsByNameInCategoryAsync(
        BrandName name,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.Brands
            .Where(b => b.Name == name.Value && b.CategoryId == categoryId);
        if (excludeId is not null)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(Slug slug, BrandId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Brands.Where(b => b.Slug == slug.Value);
        if (excludeId is not null)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.Brand.Aggregates.Brand brand, CancellationToken ct = default)
    {
        await context.Brands.AddAsync(brand, ct);
    }

    public void Update(Domain.Brand.Aggregates.Brand brand)
    {
        context.Brands.Update(brand);
    }
}