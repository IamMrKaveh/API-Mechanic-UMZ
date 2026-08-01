using Application.Common.Formatting;
using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.QueryServices;

public sealed class ReviewQueryService(DBContext context) : IReviewQueryService
{
    private const string ApprovedStatus = "Approved";

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Pending", "Approved", "Rejected", "All" };

    public async Task<PaginatedResult<ProductReviewDto>> GetApprovedProductReviewsAsync(
        ProductId productId,
        int page,
        int pageSize,
        string sortBy,
        int? minRating,
        bool verifiedOnly,
        CancellationToken ct = default)
    {
        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r =>
                r.ProductId == productId &&
                !r.IsDeleted &&
                r.User != null &&
                r.User.IsActive &&
                r.Status.Value == ApprovedStatus);

        if (minRating.HasValue && minRating.Value > 0)
        {
            var min = minRating.Value;
            query = query.Where(r => r.Rating.Value >= min);
        }

        if (verifiedOnly)
            query = query.Where(r => r.IsVerifiedPurchase);

        query = sortBy switch
        {
            "HighestRated" => query.OrderByDescending(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt),
            "LowestRated" => query.OrderBy(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var entities = await query
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);

        var items = entities.Select(MapToDto).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, safePage, safeSize);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);

        var items = entities.Select(MapToDto).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, safePage, safeSize);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        string status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize <= 0 ? 10 : pageSize;

        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        if (!AllowedStatuses.Contains(normalizedStatus))
            normalizedStatus = "Pending";

        var canonicalStatus = AllowedStatuses.First(s => s.Equals(normalizedStatus, StringComparison.OrdinalIgnoreCase));

        var query = context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => !r.IsDeleted);

        if (!string.Equals(canonicalStatus, "All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status.Value == canonicalStatus);

        var total = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);

        var items = entities.Select(MapToDto).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, safePage, safeSize);
    }

    public async Task<ReviewSummaryDto?> GetProductReviewSummaryAsync(
    ProductId productId,
    CancellationToken ct = default)
    {
        var baseQuery = context.ProductReviews
            .AsNoTracking()
            .Where(r =>
                r.ProductId == productId &&
                !r.IsDeleted &&
                r.User != null &&
                r.User.IsActive &&
                r.Status.Value == ApprovedStatus);

        var stats = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Avg = g.Average(r => (double)r.Rating.Value),
                Five = g.Count(r => r.Rating.Value == 5),
                Four = g.Count(r => r.Rating.Value == 4),
                Three = g.Count(r => r.Rating.Value == 3),
                Two = g.Count(r => r.Rating.Value == 2),
                One = g.Count(r => r.Rating.Value == 1)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
            return null;

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalReviews = stats.Total,
            TotalCount = stats.Total,
            AverageRating = Math.Round(stats.Avg, 2),
            FiveStarCount = stats.Five,
            FourStarCount = stats.Four,
            ThreeStarCount = stats.Three,
            TwoStarCount = stats.Two,
            OneStarCount = stats.One,
            RatingDistribution = new Dictionary<int, int>
            {
                [5] = stats.Five,
                [4] = stats.Four,
                [3] = stats.Three,
                [2] = stats.Two,
                [1] = stats.One
            }
        };
    }

    public async Task<ProductReviewDto?> GetByIdAsync(
        ReviewId id,
        CancellationToken ct = default)
    {
        var entity = await context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        return entity is null ? null : MapToDto(entity);
    }

    private static ProductReviewDto MapToDto(ProductReview r)
    {
        return new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = UserFullNameFormatter.Format(r.User),
            Rating = r.Rating.Value,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status.Value,
            RejectionReason = r.RejectionReason,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            LikeCount = r.LikeCount,
            DislikeCount = r.DislikeCount,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        };
    }
}
