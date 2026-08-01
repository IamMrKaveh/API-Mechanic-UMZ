using System.ComponentModel.DataAnnotations;

namespace Application.Review.Configuration;

public sealed class ReviewSettings
{
    public const string SectionName = "ReviewSettings";

    public bool RequirePurchaseVerification { get; init; } = false;

    [Range(1, 3650)]
    public int PurchaseReviewWindowDays { get; init; } = 90;

    [Range(1, 5000)]
    public int MaxAdminReplyLength { get; init; } = 1000;

    [Range(1, 5000)]
    public int MaxCommentLength { get; init; } = 1000;

    [Range(1, 500)]
    public int MinCommentLength { get; init; } = 10;

    [Range(1, 500)]
    public int MaxTitleLength { get; init; } = 100;

    [Range(1, 500)]
    public int MaxRejectionReasonLength { get; init; } = 500;

    public bool EnableLikeDislike { get; init; } = false;

    [Required]
    public ReviewRateLimitSettings RateLimit { get; init; } = new();
}
