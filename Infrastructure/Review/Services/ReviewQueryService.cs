namespace Infrastructure.Review.Services;

public class ReviewQueryService : IReviewQueryService
{
    private readonly Persistence.Context.DBContext _context;

    public ReviewQueryService(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        int productId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                     && r.Status == "Approved"
                     && !r.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User != null
                    ? ((r.User.FirstName ?? "") + " " + (r.User.LastName ?? "")).Trim()
                    : "کاربر",
                OrderId = r.OrderId,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                LikeCount = r.LikeCount,
                DislikeCount = r.DislikeCount,
                AdminReply = r.AdminReply,
                RepliedAt = r.RepliedAt,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<ProductReviewDto>.Create(reviews, totalCount, page, pageSize);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.Status == status && !r.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = r.User != null
                    ? ((r.User.FirstName ?? "") + " " + (r.User.LastName ?? "")).Trim()
                    : "کاربر",
                OrderId = r.OrderId,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                LikeCount = r.LikeCount,
                DislikeCount = r.DislikeCount,
                AdminReply = r.AdminReply,
                RepliedAt = r.RepliedAt,
                RejectionReason = r.RejectionReason,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<ProductReviewDto>.Create(reviews, totalCount, page, pageSize);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var totalCount = await query.CountAsync(ct);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                OrderId = r.OrderId,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                LikeCount = r.LikeCount,
                DislikeCount = r.DislikeCount,
                AdminReply = r.AdminReply,
                RepliedAt = r.RepliedAt,
                RejectionReason = r.RejectionReason,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<ProductReviewDto>.Create(reviews, totalCount, page, pageSize);
    }

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        int productId, CancellationToken ct = default)
    {
        var approvedReviews = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                     && r.Status == "Approved"
                     && !r.IsDeleted);

        var totalCount = await approvedReviews.CountAsync(ct);

        if (totalCount == 0)
        {
            return new ReviewSummaryDto
            {
                ProductId = productId,
                AverageRating = 0,
                TotalCount = 0
            };
        }

        var avgRating = await approvedReviews.AverageAsync(r => (decimal)r.Rating, ct);

        var ratingCounts = await approvedReviews
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new ReviewSummaryDto
        {
            ProductId = productId,
            AverageRating = Math.Round(avgRating, 1),
            TotalCount = totalCount,
            FiveStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 5)?.Count ?? 0,
            FourStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 4)?.Count ?? 0,
            ThreeStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 3)?.Count ?? 0,
            TwoStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 2)?.Count ?? 0,
            OneStarCount = ratingCounts.FirstOrDefault(r => r.Rating == 1)?.Count ?? 0
        };
    }
}