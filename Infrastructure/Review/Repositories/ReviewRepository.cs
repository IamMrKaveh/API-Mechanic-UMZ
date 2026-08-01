using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.Repositories;

public sealed class ReviewRepository(DBContext context) : IReviewRepository
{
    public async Task AddAsync(ProductReview review, CancellationToken ct = default)
        => await context.ProductReviews.AddAsync(review, ct);

    public void Update(ProductReview review)
        => context.ProductReviews.Update(review);

    public void Remove(ProductReview review)
        => context.ProductReviews.Remove(review);

    public async Task<bool> UserHasReviewedProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct)
        => await context.ProductReviews
            .AsNoTracking()
            .AnyAsync(r =>
                r.UserId == userId &&
                r.ProductId == productId &&
                !r.IsDeleted &&
                (orderId == null || r.OrderId == orderId), ct);

    public async Task<ProductReview?> GetByIdAsync(ReviewId id, CancellationToken ct = default)
        => await context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

    public async Task<ProductReview?> GetByIdIncludingDeletedAsync(ReviewId id, CancellationToken ct = default)
        => await context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<ProductReview?> GetByUserAndProductAsync(
        UserId userId,
        ProductId productId,
        OrderId? orderId,
        CancellationToken ct = default)
        => await context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .Include(r => r.Votes)
            .FirstOrDefaultAsync(r =>
                r.UserId == userId &&
                r.ProductId == productId &&
                !r.IsDeleted &&
                (orderId == null || r.OrderId == orderId), ct);

    public async Task<IReadOnlyList<ProductReview>> ListByUserAsync(
        UserId userId,
        CancellationToken ct = default)
        => await context.ProductReviews
            .Include(r => r.Product)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductReview>> ListByProductAsync(
        ProductId productId,
        CancellationToken ct = default)
        => await context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Votes)
            .Where(r => r.ProductId == productId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
