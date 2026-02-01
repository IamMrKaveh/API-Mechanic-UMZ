using Infrastructure.Persistence.Interface.Product;

namespace Infrastructure.Persistence.Repositories.Product;

public class ReviewRepository : IReviewRepository
{
    private readonly LedkaContext _context;

    public ReviewRepository(LedkaContext context)
    {
        _context = context;
    }

    public Task<bool> HasUserPurchasedProductAsync(int userId, int productId)
    {
        return _context.Set<OrderItem>()
            .AnyAsync(oi => oi.Order.UserId == userId && oi.Variant.ProductId == productId && oi.Order.IsPaid);
    }

    public async Task AddReviewAsync(ProductReview review)
    {
        await _context.Set<ProductReview>().AddAsync(review);
    }
    public void UpdateReview(ProductReview review)
    {
        _context.Set<ProductReview>().Update(review);
    }

    public Task<ProductReview?> GetReviewByIdAsync(int reviewId)
    {
        return _context.Set<ProductReview>().FindAsync(reviewId).AsTask();
    }

    public void DeleteReview(ProductReview review)
    {
        review.IsDeleted = true;
        review.DeletedAt = DateTime.UtcNow;
        _context.Set<ProductReview>().Update(review);
    }

    public async Task<(List<ProductReview> Reviews, int TotalCount)> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var query = _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId && r.Status == "Approved")
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }

    public async Task<IEnumerable<ProductReview>> GetUserReviewsAsync(int userId)
    {
        return await _context.Set<ProductReview>()
            .Where(r => r.UserId == userId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<ProductReview> Reviews, int TotalCount)> GetReviewsByStatusAsync(string status, int page, int pageSize)
    {
        var query = _context.Set<ProductReview>()
            .Where(r => r.Status == status)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (reviews, totalCount);
    }
}