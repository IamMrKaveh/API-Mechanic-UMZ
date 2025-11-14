namespace Infrastructure.Persistence.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly MechanicContext _context;

    public DiscountRepository(MechanicContext context)
    {
        _context = context;
    }

    public async Task<Domain.Discount.DiscountCode?> GetDiscountByCodeForUpdateAsync(string code)
    {
        return await _context.Set<Domain.Discount.DiscountCode>()
            .FromSqlInterpolated($"SELECT * FROM \"DiscountCode\" WHERE LOWER(\"Code\") = LOWER({code}) AND \"IsActive\" = true FOR UPDATE")
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync();
    }
}