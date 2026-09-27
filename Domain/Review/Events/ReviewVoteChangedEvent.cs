using Domain.Review.ValueObjects;

namespace Domain.Review.Events;

public sealed record ReviewVoteChangedEvent(
    ReviewId ReviewId,
    int LikeCount,
    int DislikeCount) : DomainEvent;
