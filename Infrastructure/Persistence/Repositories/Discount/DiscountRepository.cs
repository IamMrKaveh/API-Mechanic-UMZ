using Infrastructure.Persistence.Interface.Discount;

namespace Infrastructure.Persistence.Repositories.Discount;

public class DiscountRepository : IDiscountRepository
{
    private readonly LedkaContext _context;

    public DiscountRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<DiscountCode?> GetDiscountByCodeForUpdateAsync(string code)
    {
        return await _context.Set<DiscountCode>()
            .FromSqlInterpolated($"SELECT * FROM \"DiscountCode\" WHERE LOWER(\"Code\") = LOWER({code}) AND \"IsActive\" = true FOR UPDATE")
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync();
    }
}