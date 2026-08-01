using Domain.Review.Enums;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Entities;

public sealed class ReviewVote : Entity<ReviewVoteId>
{
    public ReviewId ReviewId { get; private init; } = default!;
    public UserId UserId { get; private init; } = default!;
    public VoteType Type { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    private ReviewVote()
    { }

    private ReviewVote(ReviewVoteId id, ReviewId reviewId, UserId userId, VoteType type) : base(id)
    {
        ReviewId = reviewId;
        UserId = userId;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    internal static ReviewVote Create(ReviewId reviewId, UserId userId, VoteType type)
    {
        Guard.Against.Null(reviewId, nameof(reviewId));
        Guard.Against.Null(userId, nameof(userId));

        return new ReviewVote(ReviewVoteId.NewId(), reviewId, userId, type);
    }

    internal void ChangeType(VoteType type)
    {
        if (Type == type) return;
        Type = type;
        UpdatedAt = DateTime.UtcNow;
    }
}
