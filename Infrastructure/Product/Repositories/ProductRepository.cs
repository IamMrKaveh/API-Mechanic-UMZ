using Domain.Category.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Product.Repositories;

public sealed class ProductRepository(DBContext context) : IProductRepository
{
    public async Task<Domain.Product.Aggregates.Product?> GetByIdAsync(ProductId productId, CancellationToken ct = default)
    {
        return await context.Products
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
    }

    public async Task<Domain.Product.Aggregates.Product?> GetBySlugAsync(Slug slug, CancellationToken ct = default)
    {
        return await context.Products
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Slug == slug.Value, ct);
    }

    public async Task<bool> ExistsBySlugAsync(Slug slug, ProductId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Products.Where(p => p.Slug == slug.Value);
        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Product.Aggregates.Product>> GetByCategoryIdAsync(
        CategoryId categoryId,
        CancellationToken ct = default)
    {
        var results = await context.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task AddAsync(Domain.Product.Aggregates.Product product, CancellationToken ct = default)
    {
        await context.Products.AddAsync(product, ct);
    }

    public void Update(Domain.Product.Aggregates.Product product)
    {
        context.Products.Update(product);
    }

    public void SetOriginalRowVersion(Domain.Product.Aggregates.Product product, byte[] rowVersion)
    {
        context.Entry(product).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}