namespace Application.Review.Features.Shared;

public record ProductReviewDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public int UserId { get; init; }
    public string? UserName { get; init; }
    public int? OrderId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Comment { get; init; }
    public string Status { get; init; } = "Pending";
    public bool IsVerifiedPurchase { get; init; }
    public int LikeCount { get; init; }
    public int DislikeCount { get; init; }
    public string? AdminReply { get; init; }
    public DateTime? RepliedAt { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ReviewSummaryDto
{
    public int ProductId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalCount { get; init; }
    public int FiveStarCount { get; init; }
    public int FourStarCount { get; init; }
    public int ThreeStarCount { get; init; }
    public int TwoStarCount { get; init; }
    public int OneStarCount { get; init; }
}