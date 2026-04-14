using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Review.Repositories;

public sealed class ReviewRepository(DBContext context) : IReviewRepository
{
    public async Task<ProductReview?> GetByIdAsync(ReviewId reviewId, CancellationToken ct = default)
    {
        return await context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);
    }

    public async Task<bool> HasUserReviewedProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
    {
        return await context.ProductReviews
            .AnyAsync(r => r.UserId == userId && r.ProductId == productId, ct);
    }

    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(
        ProductId productId,
        bool approvedOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.ProductReviews
            .Where(r => r.ProductId == productId);

        if (approvedOnly)
            query = query.Where(r => r.IsApproved);

        var results = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task AddAsync(ProductReview review, CancellationToken ct = default)
    {
        await context.ProductReviews.AddAsync(review, ct);
    }

    public void Update(ProductReview review)
    {
        context.ProductReviews.Update(review);
    }
}