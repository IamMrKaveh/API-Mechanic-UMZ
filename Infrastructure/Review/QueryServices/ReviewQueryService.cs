using Application.Audit.Contracts;
using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Order.ValueObjects;
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
                RejectionReason = r.RejectionReason,
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
                RejectionReason = r.RejectionReason,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductReviewDto>(items, total, page, pageSize);
    }

    public async Task<ReviewSummaryDto?> GetProductReviewSummaryAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var reviews = await context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId
                && r.Status == ReviewStatus.Approved
                && !r.IsDeleted)
            .ToListAsync(ct);

        if (reviews.Count == 0) return null;

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalCount = reviews.Count,
            AverageRating = reviews.Average(r => r.Rating.Value),
            FiveStarCount = reviews.Count(r => r.Rating.Value == 5),
            FourStarCount = reviews.Count(r => r.Rating.Value == 4),
            ThreeStarCount = reviews.Count(r => r.Rating.Value == 3),
            TwoStarCount = reviews.Count(r => r.Rating.Value == 2),
            OneStarCount = reviews.Count(r => r.Rating.Value == 1)
        };
    }

    public Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(string status, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}