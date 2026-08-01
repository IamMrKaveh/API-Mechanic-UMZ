using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed class ReviewVoteChangedEvent(
    ReviewId reviewId,
    int likeCount,
    int dislikeCount)
    : DomainEvent
{
    public ReviewId ReviewId { get; } = reviewId;
    public int LikeCount { get; } = likeCount;
    public int DislikeCount { get; } = dislikeCount;
}
