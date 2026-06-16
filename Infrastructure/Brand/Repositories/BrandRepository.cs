using Domain.Brand.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.Repositories;

public sealed class BrandRepository(DBContext context) : IBrandRepository
{
    private const string ConcurrencyTokenName = "xmin";

    public async Task<Domain.Brand.Aggregates.Brand?> GetByIdAsync(
        BrandId brandId,
        CancellationToken ct = default)
        => await context.Brands.FirstOrDefaultAsync(b => b.Id == brandId, ct);

    public async Task<bool> ExistsByNameInCategoryAsync(
        BrandName name,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default)
    {
        var nameValue = name.Value;
        var query = context.Brands
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
        var query = context.Brands.Where(b => b.Slug.Value == slug.Value);
        if (excludeId is not null)
            query = query.Where(b => b.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        Domain.Brand.Aggregates.Brand brand,
        CancellationToken ct = default)
        => await context.Brands.AddAsync(brand, ct);

    public void Update(Domain.Brand.Aggregates.Brand brand)
        => context.Brands.Update(brand);

    public void SetOriginalRowVersion(
        Domain.Brand.Aggregates.Brand entity,
        byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;

        var token = ToConcurrencyToken(rowVersion);
        context.Entry(entity).Property<uint>(ConcurrencyTokenName).OriginalValue = token;
    }

    public byte[]? GetCurrentRowVersion(Domain.Brand.Aggregates.Brand entity)
    {
        var token = context.Entry(entity).Property<uint>(ConcurrencyTokenName).CurrentValue;
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