namespace Infrastructure.Persistence.Repositories;

public class DiscountRepository : IDiscountRepository
{
    private readonly LedkaContext _context;

    public DiscountRepository(LedkaContext context)
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