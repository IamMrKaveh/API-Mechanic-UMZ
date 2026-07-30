using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.QueryServices;

public sealed class ReviewQueryService(DBContext context) : IReviewQueryService
{
    private const string DeletedUserDisplayName = "کاربر حذف‌شده";
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
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Where(r =>
                r.ProductId == productId &&
                !r.IsDeleted &&
                r.User != null &&
                r.User.IsActive);

        query = query.Where(r => r.Status.Value == ApprovedStatus);

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
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        ProductId productId,
        CancellationToken ct = default)
    {
        var baseQuery = context.ProductReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Where(r =>
                r.ProductId == productId &&
                !r.IsDeleted &&
                r.User != null &&
                r.User.IsActive)
            .Where(r => r.Status.Value == ApprovedStatus);

        var ratings = await baseQuery
            .Select(r => r.Rating.Value)
            .ToListAsync(ct);

        if (ratings.Count == 0)
        {
            return new ReviewSummaryDto
            {
                ProductId = productId.Value,
                TotalReviews = 0,
                TotalCount = 0,
                AverageRating = 0,
                FiveStarCount = 0,
                FourStarCount = 0,
                ThreeStarCount = 0,
                TwoStarCount = 0,
                OneStarCount = 0,
                RatingDistribution = new Dictionary<int, int>
                {
                    [1] = 0,
                    [2] = 0,
                    [3] = 0,
                    [4] = 0,
                    [5] = 0
                }
            };
        }

        var five = ratings.Count(v => v == 5);
        var four = ratings.Count(v => v == 4);
        var three = ratings.Count(v => v == 3);
        var two = ratings.Count(v => v == 2);
        var one = ratings.Count(v => v == 1);
        var avg = ratings.Average();

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalReviews = ratings.Count,
            TotalCount = ratings.Count,
            AverageRating = Math.Round(avg, 2),
            FiveStarCount = five,
            FourStarCount = four,
            ThreeStarCount = three,
            TwoStarCount = two,
            OneStarCount = one,
            RatingDistribution = new Dictionary<int, int>
            {
                [5] = five,
                [4] = four,
                [3] = three,
                [2] = two,
                [1] = one
            }
        };
    }

    public async Task<ProductReviewDto?> GetByIdAsync(
        ReviewId id,
        CancellationToken ct = default)
    {
        var entity = await context.ProductReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        return entity is null ? null : MapToDto(entity);
    }

    private static ProductReviewDto MapToDto(ProductReview r)
    {
        var firstName = r.User?.FullName?.FirstName;
        var lastName = r.User?.FullName?.LastName;

        return new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = BuildFullName(firstName, lastName),
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

    private static string BuildFullName(string? firstName, string? lastName)
    {
        var first = (firstName ?? string.Empty).Trim();
        var last = (lastName ?? string.Empty).Trim();
        var full = $"{first} {last}".Trim();
        return string.IsNullOrWhiteSpace(full) ? DeletedUserDisplayName : full;
    }
}
