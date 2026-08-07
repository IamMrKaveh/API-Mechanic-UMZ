using Application.Common.Formatting;
using Application.Review.Contracts;
using Application.Review.Features.Queries.AdminReviewStats;
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
        UserId? currentUserId,
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

        var votes = await LoadUserVotesAsync(entities, currentUserId, ct);
        var items = entities.Select(r => MapToDto(r, votes)).ToList();

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

    public async Task<PaginatedResult<ProductReviewDto>> GetReviewsByStatusAsync(
        AdminReviewFilter filter,
        CancellationToken cancellationToken)
    {
        var query = context.ProductReviews
            .AsNoTracking()
            .Include(r => r.Product)
            .Include(r => r.User)
            .Where(r => !r.IsDeleted);

        if (!string.Equals(filter.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            var statusValue = ReviewStatus.From(filter.Status);
            query = query.Where(r => r.Status == statusValue);
        }

        if (filter.ProductId.HasValue && filter.ProductId.Value != Guid.Empty)
        {
            var pid = filter.ProductId;
            query = query.Where(r => r.ProductId == pid);
        }

        if (filter.MinRating.HasValue)
        {
            var min = filter.MinRating;
            query = query.Where(r => r.Rating >= min);
        }

        if (filter.DateFrom.HasValue)
        {
            var from = filter.DateFrom.Value.ToUniversalTime();
            query = query.Where(r => r.CreatedAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.ToUniversalTime();
            query = query.Where(r => r.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var q = filter.SearchText.Trim().ToLower();
            query = query.Where(r =>
                (r.Title != null && r.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase)) ||
                r.Comment!.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                (r.User != null &&
                    ((r.User.FullName.FirstName != null && r.User.FullName.FirstName.Contains(q, StringComparison.CurrentCultureIgnoreCase)) ||
                     (r.User.FullName.LastName != null && r.User.FullName.LastName.Contains(q, StringComparison.CurrentCultureIgnoreCase)))));
        }

        var total = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(r => MapToDto(r, null)).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<AdminReviewStatsDto> GetAdminReviewStatsAsync(CancellationToken cancellationToken)
    {
        var grouped = await context.ProductReviews
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var pending = grouped.FirstOrDefault(g => g.Status == ReviewStatus.Pending)?.Count ?? 0;
        var approved = grouped.FirstOrDefault(g => g.Status == ReviewStatus.Approved)?.Count ?? 0;
        var rejected = grouped.FirstOrDefault(g => g.Status == ReviewStatus.Rejected)?.Count ?? 0;

        return new AdminReviewStatsDto(pending, approved, rejected, pending + approved + rejected);
    }

    public async Task<ProductReviewDto?> GetByIdAsync(
        ReviewId reviewId,
        UserId? currentUserId,
        CancellationToken cancellationToken)
    {
        var review = await context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted, cancellationToken);

        if (review is null) return null;

        Dictionary<Guid, string>? votes = null;
        if (currentUserId is not null)
        {
            var vote = await context.Set<Domain.Review.Entities.ReviewVote>()
                .AsNoTracking()
                .Where(v => v.ReviewId == reviewId && v.UserId == currentUserId)
                .Select(v => v.Type)
                .FirstOrDefaultAsync(cancellationToken);

            if (vote != default)
            {
                votes = new Dictionary<Guid, string>
                {
                    [review.Id.Value] = vote.ToString()
                };
            }
        }

        return MapToDto(review, votes);
    }

    public async Task<PaginatedResult<ProductReviewDto>> GetUserReviewsAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safeSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(cancellationToken);

        var entities = await query
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(r => MapToDto(r, null)).ToList();

        return new PaginatedResult<ProductReviewDto>(items, total, safePage, safeSize);
    }

    private async Task<Dictionary<Guid, string>?> LoadUserVotesAsync(
        IReadOnlyCollection<ProductReview> entities,
        UserId? currentUserId,
        CancellationToken ct)
    {
        if (currentUserId is null || entities.Count == 0)
            return null;

        var reviewIds = entities.Select(r => r.Id).ToList();

        var votes = await context.Set<Domain.Review.Entities.ReviewVote>()
            .AsNoTracking()
            .Where(v => reviewIds.Contains(v.ReviewId) && v.UserId == currentUserId)
            .Select(v => new { ReviewId = v.ReviewId.Value, Type = v.Type })
            .ToListAsync(ct);

        if (votes.Count == 0) return null;

        return votes.ToDictionary(v => v.ReviewId, v => v.Type.ToString());
    }

    private static ProductReviewDto MapToDto(ProductReview r, IReadOnlyDictionary<Guid, string>? votes)
    {
        string? userVote = null;
        if (votes is not null && votes.TryGetValue(r.Id.Value, out var voteValue))
            userVote = voteValue;

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
            UserVote = userVote,
            CreatedAt = r.CreatedAt,
            OrderId = r.OrderId?.Value
        };
    }
}
