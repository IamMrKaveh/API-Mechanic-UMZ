namespace Infrastructure.Review.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly LedkaContext _context;

    public ReviewRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<ProductReview?> GetByIdAsync(int reviewId, CancellationToken ct = default)
    {
        return await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);
    }

    public async Task<bool> UserHasReviewedProductAsync(
        int userId, int productId, int? orderId = null, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .Where(r => r.UserId == userId && r.ProductId == productId && !r.IsDeleted);

        if (orderId.HasValue)
            query = query.Where(r => r.OrderId == orderId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> UserHasPurchasedProductAsync(
        int userId, int productId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId && !o.IsDeleted && o.PaymentDate != null)
            .AnyAsync(o => o.OrderItems.Any(oi => oi.ProductId == productId), ct);
    }

    public async Task AddAsync(ProductReview review, CancellationToken ct = default)
    {
        await _context.ProductReviews.AddAsync(review, ct);
    }

    public void Update(ProductReview review)
    {
        _context.ProductReviews.Update(review);
    }

    public async Task<(IEnumerable<ProductReview> Reviews, int TotalCount)> GetByProductIdAsync(
        int productId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .Where(r => r.ProductId == productId && !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.User)
            .ToListAsync(ct);

        return (reviews, totalCount);
    }

    public async Task<(IEnumerable<ProductReview> Reviews, int TotalCount)> GetPendingReviewsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .Where(r => r.Status == "Pending" && !r.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.User)
            .Include(r => r.Product)
            .ToListAsync(ct);

        return (reviews, totalCount);
    }
}