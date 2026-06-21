using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.QueryServices;

public sealed class ReviewQueryService(DBContext context) : IReviewQueryService
{
    public async Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        ProductId productId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                && r.Status == ReviewStatus.Approved
                && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id.Value,
                ProductId = r.ProductId.Value,
                UserId = r.UserId.Value,
                Rating = r.Rating.Value,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status.Value,
                AdminReply = r.AdminReply,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductReviewDto>(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        UserId userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id.Value,
                ProductId = r.ProductId.Value,
                UserId = r.UserId.Value,
                Rating = r.Rating.Value,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status.Value,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductReviewDto>(items, total, page, pageSize);
    }

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var summary = await context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                && r.Status == ReviewStatus.Approved
                && !r.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                AverageRating = g.Average(r => r.Rating.Value),
                FiveStar = g.Count(r => r.Rating.Value == 5),
                FourStar = g.Count(r => r.Rating.Value == 4),
                ThreeStar = g.Count(r => r.Rating.Value == 3),
                TwoStar = g.Count(r => r.Rating.Value == 2),
                OneStar = g.Count(r => r.Rating.Value == 1)
            })
            .FirstOrDefaultAsync(ct);

        if (summary is null || summary.Total == 0)
        {
            return new ReviewSummaryDto
            {
                ProductId = productId.Value,
                TotalReviews = 0,
                TotalCount = 0,
                AverageRating = 0,
                RatingDistribution = new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 }
            };
        }

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalReviews = summary.Total,
            TotalCount = summary.Total,
            AverageRating = summary.AverageRating,
            FiveStarCount = summary.FiveStar,
            FourStarCount = summary.FourStar,
            ThreeStarCount = summary.ThreeStar,
            TwoStarCount = summary.TwoStar,
            OneStarCount = summary.OneStar,
            RatingDistribution = new Dictionary<int, int>
            {
                [5] = summary.FiveStar,
                [4] = summary.FourStar,
                [3] = summary.ThreeStar,
                [2] = summary.TwoStar,
                [1] = summary.OneStar
            }
        };
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Where(r => r.Status.Value == status && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id.Value,
                ProductId = r.ProductId.Value,
                UserId = r.UserId.Value,
                Rating = r.Rating.Value,
                Title = r.Title,
                Comment = r.Comment,
                Status = r.Status.Value,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductReviewDto>(items, total, page, pageSize);
    }
}