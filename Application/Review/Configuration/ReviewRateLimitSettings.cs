using System.ComponentModel.DataAnnotations;

namespace Application.Review.Configuration;

public sealed class ReviewRateLimitSettings
{
    [Range(1, 1000)]
    public int CreateReviewPerMinute { get; init; } = 5;

    [Range(1, 10000)]
    public int PublicReadsPerMinute { get; init; } = 60;

    [Range(1, 1000)]
    public int AdminActionsPerMinute { get; init; } = 30;

    [Range(1, 1000)]
    public int VotePerMinute { get; init; } = 20;
}
