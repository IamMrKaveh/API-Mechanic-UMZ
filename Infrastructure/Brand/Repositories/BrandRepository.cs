using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.Repositories;

public sealed class BrandRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Domain.Brand.Aggregates.Brand, BrandId>(context), IBrandRepository
{
    private const string ConcurrencyTokenName = "xmin";

    public async Task<bool> ExistsByNameInCategoryAsync(
        BrandName name,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default)
    {
        var nameValue = name.Value;
        var query = Context.Brands
            .Where(b => b.Name.Value == nameValue && b.CategoryId == categoryId);
        if (excludeId is not null)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(
        BrandSlug slug,
        BrandId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = Context.Brands.Where(b => b.Slug.Value == slug.Value);
        if (excludeId is not null)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public void Update(Domain.Brand.Aggregates.Brand brand, byte[]? rowVersion = null)
    {
        base.Update(brand);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(brand, rowVersion);
    }

    public void SetOriginalRowVersion(
        Domain.Brand.Aggregates.Brand entity,
        byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;

        var token = ToConcurrencyToken(rowVersion);
        Context.Entry(entity).Property<uint>(ConcurrencyTokenName).OriginalValue = token;
    }

    public byte[]? GetCurrentRowVersion(Domain.Brand.Aggregates.Brand entity)
    {
        var token = Context.Entry(entity).Property<uint>(ConcurrencyTokenName).CurrentValue;
        return FromConcurrencyToken(token);
    }

    private static uint ToConcurrencyToken(byte[] rowVersion)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        var length = Math.Min(rowVersion.Length, buffer.Length);
        rowVersion.AsSpan(0, length).CopyTo(buffer);
        return BitConverter.ToUInt32(buffer);
    }

    private static byte[] FromConcurrencyToken(uint token)
        => BitConverter.GetBytes(token);
}
