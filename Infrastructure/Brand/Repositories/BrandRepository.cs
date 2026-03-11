using Domain.Brand.Interfaces;

namespace Infrastructure.Brand.Repositories;

public class BrandRepository(DBContext context) : IBrandRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        Domain.Brand.Aggregates.Brand brand,
        CancellationToken ct = default)
    {
        await _context.Brands.AddAsync(brand, ct);
    }

    public void Update(Domain.Brand.Aggregates.Brand brand)
    {
        _context.Brands.Update(brand);
    }

    public void SetOriginalRowVersion(
        Domain.Brand.Aggregates.Brand entity,
        byte[] rowVersion)
    {
        _context.Entry(entity).Property(b => b.RowVersion).OriginalValue = rowVersion;
    }
}