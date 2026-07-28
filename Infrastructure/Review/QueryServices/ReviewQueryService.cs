using Application.Review.Contracts;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
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
            .Where(r =>
                r.ProductId == productId &&
                r.Status.Value == ApprovedStatus &&
                !r.IsDeleted);

        if (minRating.HasValue && minRating.Value > 0)
            query = query.Where(r => r.Rating.Value >= minRating.Value);

        if (verifiedOnly)
            query = query.Where(r => r.IsVerifiedPurchase);

        query = sortBy switch
        {
            "HighestRated" => query.OrderByDescending(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt),
            "LowestRated" => query.OrderBy(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var projected = query
            .Where(r => r.User != null && r.User.IsActive)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.UserId,
                FirstName = r.User != null && r.User.FullName != null ? r.User.FullName.FirstName : null,
                LastName = r.User != null && r.User.FullName != null ? r.User.FullName.LastName : null,
                Rating = r.Rating.Value,
                r.Title,
                r.Comment,
                Status = r.Status.Value,
                r.RejectionReason,
                r.AdminReply,
                r.RepliedAt,
                r.IsVerifiedPurchase,
                r.LikeCount,
                r.DislikeCount,
                r.CreatedAt,
                r.OrderId
            });

        var total = await projected.CountAsync(ct);

        var rows = await projected
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(ct);

        var items = rows.Select(r => new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = BuildFullName(r.FirstName, r.LastName),
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status,
            RejectionReason = r.RejectionReason,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            LikeCount = r.LikeCount,
            DislikeCount = r.DislikeCount,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        }).ToList();

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
            .Where(r => r.UserId == userId && !r.IsDeleted);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.UserId,
                FirstName = r.User != null && r.User.FullName != null ? r.User.FullName.FirstName : null,
                LastName = r.User != null && r.User.FullName != null ? r.User.FullName.LastName : null,
                Rating = r.Rating.Value,
                r.Title,
                r.Comment,
                Status = r.Status.Value,
                r.RejectionReason,
                r.AdminReply,
                r.RepliedAt,
                r.IsVerifiedPurchase,
                r.LikeCount,
                r.DislikeCount,
                r.CreatedAt,
                r.OrderId
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = BuildFullName(r.FirstName, r.LastName),
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status,
            RejectionReason = r.RejectionReason,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            LikeCount = r.LikeCount,
            DislikeCount = r.DislikeCount,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        }).ToList();

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
            .Where(r => !r.IsDeleted);

        if (!string.Equals(canonicalStatus, "All", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status.Value == canonicalStatus);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.UserId,
                FirstName = r.User != null && r.User.FullName != null ? r.User.FullName.FirstName : null,
                LastName = r.User != null && r.User.FullName != null ? r.User.FullName.LastName : null,
                Rating = r.Rating.Value,
                r.Title,
                r.Comment,
                Status = r.Status.Value,
                r.RejectionReason,
                r.AdminReply,
                r.RepliedAt,
                r.IsVerifiedPurchase,
                r.LikeCount,
                r.DislikeCount,
                r.CreatedAt,
                r.OrderId
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ProductReviewDto
        {
            Id = r.Id.Value,
            ProductId = r.ProductId.Value,
            UserId = r.UserId.Value,
            UserFullName = BuildFullName(r.FirstName, r.LastName),
            Rating = r.Rating,
            Title = r.Title,
            Comment = r.Comment,
            Status = r.Status,
            RejectionReason = r.RejectionReason,
            AdminReply = r.AdminReply,
            RepliedAt = r.RepliedAt,
            IsVerifiedPurchase = r.IsVerifiedPurchase,
            LikeCount = r.LikeCount,
            DislikeCount = r.DislikeCount,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        }).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, safePage, safeSize);
    }

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(
        ProductId productId,
        CancellationToken ct = default)
    {
        var baseQuery = context.ProductReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r =>
                r.ProductId == productId &&
                r.Status.Value == ApprovedStatus &&
                !r.IsDeleted &&
                r.User != null &&
                r.User.IsActive);

        var summary = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                AverageRating = g.Average(r => (double)r.Rating.Value),
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

        return new ReviewSummaryDto
        {
            ProductId = productId.Value,
            TotalReviews = summary.Total,
            TotalCount = summary.Total,
            AverageRating = Math.Round(summary.AverageRating, 2),
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

    public async Task<ProductReviewDto?> GetByIdAsync(
        ReviewId id,
        CancellationToken ct = default)
    {
        var row = await context.ProductReviews
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.ProductId,
                r.UserId,
                FirstName = r.User != null && r.User.FullName != null ? r.User.FullName.FirstName : null,
                LastName = r.User != null && r.User.FullName != null ? r.User.FullName.LastName : null,
                Rating = r.Rating.Value,
                r.Title,
                r.Comment,
                Status = r.Status.Value,
                r.RejectionReason,
                r.AdminReply,
                r.RepliedAt,
                r.IsVerifiedPurchase,
                r.LikeCount,
                r.DislikeCount,
                r.CreatedAt,
                r.OrderId
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        return new ProductReviewDto
        {
            Id = row.Id.Value,
            ProductId = row.ProductId.Value,
            UserId = row.UserId.Value,
            UserFullName = BuildFullName(row.FirstName, row.LastName),
            Rating = row.Rating,
            Title = row.Title,
            Comment = row.Comment,
            Status = row.Status,
            RejectionReason = row.RejectionReason,
            AdminReply = row.AdminReply,
            RepliedAt = row.RepliedAt,
            IsVerifiedPurchase = row.IsVerifiedPurchase,
            LikeCount = row.LikeCount,
            DislikeCount = row.DislikeCount,
            CreatedAt = row.CreatedAt,
            OrderId = row.OrderId?.Value
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
