using Domain.Brand.ValueObjects;
using Domain.Product.Aggregates;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ValueObjects;

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

    public async Task<Domain.Product.Aggregates.Product?> GetBySlugAsync(Slug slug, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Slug == slug.Value, ct);

    public async Task<bool> ExistsAsync(ProductId id, CancellationToken ct = default)
        => await context.Products.AnyAsync(p => p.Id == id && !p.IsDeleted, ct);

    public async Task<bool> ExistsBySlugAsync(Slug slug, ProductId? excludeId = null, CancellationToken ct = default)
        => await context.Products
            .AnyAsync(p => p.Slug == slug.Value
                && !p.IsDeleted
                && (excludeId == null || p.Id != excludeId), ct);

    public async Task<IReadOnlyList<Domain.Product.Aggregates.Product>> GetByCategoryIdAsync(
        Domain.Category.ValueObjects.CategoryId categoryId, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Brand)
            .Where(p => p.Brand.CategoryId == categoryId && !p.IsDeleted)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Domain.Product.Aggregates.Product>> GetByBrandIdAsync(
        BrandId brandId, CancellationToken ct = default)
        => await context.Products
            .Include(p => p.Brand)
            .Where(p => p.BrandId == brandId && !p.IsDeleted)
            .ToListAsync(ct);
}