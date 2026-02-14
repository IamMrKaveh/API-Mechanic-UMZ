namespace Application.Product.Features.Shared;

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsVerifiedPurchase { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewSummaryDto
{
    public int ProductId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalCount { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
}