using System.Buffers.Binary;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;

namespace Infrastructure.Product.Repositories;

public sealed class ProductRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Domain.Product.Aggregates.Product, ProductId>(context), IProductRepository
{
    private const string ConcurrencyTokenName = "xmin";

    public void Update(Domain.Product.Aggregates.Product product, byte[]? rowVersion = null)
    {
        base.Update(product);

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

        Context.Entry(entity).Property<uint>(ConcurrencyTokenName).OriginalValue = xmin;
    }

    public override async Task<Domain.Product.Aggregates.Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => await Context.Products
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> ExistsBySlugAsync(ProductSlug slug, ProductId? excludeId = null, CancellationToken ct = default)
        => await Context.Products
            .AnyAsync(p => p.Slug.Value == slug.Value
                && !p.IsDeleted
                && (excludeId == null || p.Id != excludeId), ct);
}
