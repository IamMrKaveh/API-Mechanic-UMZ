using Application.Audit.Contracts;
using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Review.QueryServices;

public sealed class ReviewQueryService(
    DBContext context,
    IAuditService auditService) : IReviewQueryService
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

    public async Task<PaginatedResult<ProductReviewDto>> GetPendingReviewsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Pending && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(r => r.CreatedAt)
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
        var reviews = await context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                && r.Status == ReviewStatus.Approved
                && !r.IsDeleted)
            .ToListAsync(ct);

        var total = reviews.Count;

        if (total == 0)
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

        var fiveStar = reviews.Count(r => r.Rating.Value == 5);
        var fourStar = reviews.Count(r => r.Rating.Value == 4);
        var threeStar = reviews.Count(r => r.Rating.Value == 3);
        var twoStar = reviews.Count(r => r.Rating.Value == 2);
        var oneStar = reviews.Count(r => r.Rating.Value == 1);

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalReviews = total,
            TotalCount = total,
            AverageRating = reviews.Average(r => r.Rating.Value),
            FiveStarCount = fiveStar,
            FourStarCount = fourStar,
            ThreeStarCount = threeStar,
            TwoStarCount = twoStar,
            OneStarCount = oneStar,
            RatingDistribution = new Dictionary<int, int>
            {
                [5] = fiveStar,
                [4] = fourStar,
                [3] = threeStar,
                [2] = twoStar,
                [1] = oneStar
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