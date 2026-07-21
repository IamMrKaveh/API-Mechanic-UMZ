using System.Buffers.Binary;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Infrastructure.Product.Repositories;

public sealed class ProductRepository(DBContext context) : IProductRepository
{
    private const string ConcurrencyTokenName = "xmin";

    public async Task AddAsync(Domain.Product.Aggregates.Product product, CancellationToken ct = default)
        => await context.Products.AddAsync(product, ct);

    public void Update(Domain.Product.Aggregates.Product product, byte[]? rowVersion = null)
    {
        context.Products.Update(product);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(product, rowVersion);
    }

    public void SetOriginalRowVersion(Domain.Product.Aggregates.Product entity, byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;

        var xmin = rowVersion.Length >= 4
            ? BinaryPrimitives.ReadUInt32BigEndian(rowVersion.AsSpan(0, 4))
            : 0u;

        context.Entry(entity).Property<uint>(ConcurrencyTokenName).OriginalValue = xmin;
    }

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
