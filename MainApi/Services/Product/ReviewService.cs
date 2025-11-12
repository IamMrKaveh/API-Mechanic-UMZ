namespace MainApi.Services.Product;

public class ReviewService : IReviewService
{
    private readonly MechanicContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(MechanicContext context, ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductReviewDto> CreateReviewAsync(CreateReviewDto dto, int userId)
    {
        var hasPurchased = await _context.TOrderItems
            .AnyAsync(oi => oi.Order.UserId == userId && oi.Variant.ProductId == dto.ProductId && oi.Order.IsPaid);

        var review = new TProductReview
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Title = dto.Title,
            Comment = dto.Comment,
            Status = "Pending",
            IsVerifiedPurchase = hasPurchased,
            CreatedAt = DateTime.UtcNow
        };

        _context.TProductReview.Add(review);
        await _context.SaveChangesAsync();

        return MapToDto(review);
    }

    public async Task<IEnumerable<ProductReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        return await _context.TProductReview
            .Where(r => r.ProductId == productId && r.Status == "Approved")
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User.FirstName + " " + r.User.LastName,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductReviewDto>> GetUserReviewsAsync(int userId)
    {
        return await _context.TProductReview
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User.FirstName + " " + r.User.LastName,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateReviewStatusAsync(int reviewId, string status)
    {
        var review = await _context.TProductReview.FindAsync(reviewId);
        if (review == null) return false;

        review.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteReviewAsync(int reviewId)
    {
        var review = await _context.TProductReview.FindAsync(reviewId);
        if (review == null) return false;

        _context.TProductReview.Remove(review);
        await _context.SaveChangesAsync();
        return true;
    }

    private static ProductReviewDto MapToDto(TProductReview review)
    {
        return new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = review.User?.FirstName + " " + review.User?.LastName,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            IsVerifiedPurchase = review.IsVerifiedPurchase
        };
    }
}