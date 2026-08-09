using Domain.Review.Entities;
using Domain.Review.Enums;
using Domain.Review.Events;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Entities;

public class ReviewVoteTests
{
    [Fact]
    public void AddLike_PersistsVoteWithLikeType()
    {
        var author = UserId.NewId();
        var voter = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(author).BuildApproved();

        review.AddLike(voter);

        var vote = review.Votes.ShouldHaveSingleItem();
        vote.UserId.ShouldBe(voter);
        vote.ReviewId.ShouldBe(review.Id);
        vote.Type.ShouldBe(VoteType.Like);
        vote.Id.ShouldNotBeNull();
        vote.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void AddLike_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();

        review.AddLike(UserId.NewId());

        var after = DateTime.UtcNow.AddSeconds(1);
        var vote = review.Votes.Single();
        vote.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        vote.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        vote.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeType_ViaSwitchingVote_UpdatesTypeAndSetsUpdatedAt()
    {
        var voter = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.AddLike(voter);

        review.AddDislike(voter);

        var vote = review.Votes.ShouldHaveSingleItem();
        vote.Type.ShouldBe(VoteType.Dislike);
        vote.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangeType_ToSameType_LeavesUpdatedAtNull()
    {
        var voter = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.AddLike(voter);
        review.ClearDomainEvents();

        review.AddLike(voter);

        var vote = review.Votes.ShouldHaveSingleItem();
        vote.Type.ShouldBe(VoteType.Like);
        vote.UpdatedAt.ShouldBeNull();
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddLike_ThenAddDislikeForSameUser_RaisesTwoVoteChangedEventsButKeepsSingleVoteInstance()
    {
        var voter = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.ClearDomainEvents();

        review.AddLike(voter);
        review.AddDislike(voter);

        review.Votes.Count.ShouldBe(1);
        review.DomainEvents.Count(e => e is ReviewVoteChangedEvent).ShouldBe(2);
    }

    [Fact]
    public void Votes_CollectionIsReadOnly()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddLike(UserId.NewId());

        review.Votes.ShouldBeAssignableTo<IReadOnlyCollection<ReviewVote>>();
    }
}
