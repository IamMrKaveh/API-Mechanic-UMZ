using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Review.Repositories;

public sealed class ReviewRepository(DBContext context) : IReviewRepository
{
    public async Task AddAsync(ProductReview review, CancellationToken ct = default)
        => await context.ProductReviews.AddAsync(review, ct);

    public void Update(ProductReview review)
        => context.ProductReviews.Update(review);

    public async Task<bool> UserHasReviewedProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct)
        => await context.ProductReviews
            .AnyAsync(r =>
                r.UserId == userId &&
                r.ProductId == productId &&
                !r.IsDeleted &&
                (orderId == null || r.OrderId == orderId), ct);

    public async Task<ProductReview?> GetByIdAsync(ReviewId id, CancellationToken ct = default)
        => await context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
}