using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Infrastructure.Product.Repositories;

public sealed class ProductRepository(DBContext context) : IProductRepository
{
    public async Task AddAsync(Domain.Product.Aggregates.Product product, CancellationToken ct = default)
        => await context.Products.AddAsync(product, ct);

    public void Update(Domain.Product.Aggregates.Product product)
        => context.Products.Update(product);

    public void SetOriginalRowVersion(Domain.Product.Aggregates.Product entity, byte[] rowVersion)
        => context.Entry(entity).Property<byte[]>("RowVersion").OriginalValue = rowVersion;

    public async Task<Domain.Product.Aggregates.Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> ExistsBySlugAsync(ProductSlug slug, ProductId? excludeId = null, CancellationToken ct = default)
        => await context.Products
            .AnyAsync(p => p.Slug.Value == slug.Value
                && !p.IsDeleted
                && (excludeId == null || p.Id != excludeId), ct);
}