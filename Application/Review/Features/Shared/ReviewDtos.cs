using Domain.Order.ValueObjects;

namespace Application.Review.Features.Shared;

public record ProductReviewDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public bool IsVerifiedPurchase { get; init; }
    public int LikeCount { get; init; }
    public int DislikeCount { get; init; }
    public string? AdminReply { get; init; }
    public DateTime? RepliedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderId? OrderId { get; init; }
}

public record ReviewSummaryDto
{
    public Guid ProductId { get; init; }
    public double AverageRating { get; init; }
    public int TotalReviews { get; init; }
    public int TotalCount { get; init; }
    public int FiveStarCount { get; init; }
    public int FourStarCount { get; init; }
    public int ThreeStarCount { get; init; }
    public int TwoStarCount { get; init; }
    public int OneStarCount { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = [];
}

public record CreateReviewDto
{
    public Guid ProductId { get; init; }
    public Guid? OrderId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
}