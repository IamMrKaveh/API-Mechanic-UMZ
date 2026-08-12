using Domain.Review.Enums;
using Domain.Review.Events;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewVotingTests
{
    [Fact]
    public void AddLike_OnApprovedReview_IncrementsLikeCountAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.ClearDomainEvents();
        var voter = UserId.NewId();

        review.AddLike(voter);

        review.LikeCount.ShouldBe(1);
        review.DislikeCount.ShouldBe(0);
        review.Votes.Count.ShouldBe(1);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewVoteChangedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.LikeCount.ShouldBe(1);
        evt.DislikeCount.ShouldBe(0);
    }

    [Fact]
    public void AddDislike_OnApprovedReview_IncrementsDislikeCountAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.ClearDomainEvents();

        review.AddDislike(UserId.NewId());

        review.DislikeCount.ShouldBe(1);
        review.LikeCount.ShouldBe(0);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewVoteChangedEvent>();
        evt.LikeCount.ShouldBe(0);
        evt.DislikeCount.ShouldBe(1);
    }

    [Fact]
    public void AddLike_TwiceBySameUser_IsIdempotent()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        var voter = UserId.NewId();
        review.AddLike(voter);
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.AddLike(voter);

        review.LikeCount.ShouldBe(1);
        review.Votes.Count.ShouldBe(1);
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddDislike_AfterLikeBySameUser_SwitchesVoteAndKeepsSingleVote()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        var voter = UserId.NewId();
        review.AddLike(voter);
        review.ClearDomainEvents();

        review.AddDislike(voter);

        review.LikeCount.ShouldBe(0);
        review.DislikeCount.ShouldBe(1);
        review.Votes.Count.ShouldBe(1);
        review.Votes.Single().Type.ShouldBe(VoteType.Dislike);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewVoteChangedEvent>();
        evt.LikeCount.ShouldBe(0);
        evt.DislikeCount.ShouldBe(1);
    }

    [Fact]
    public void AddLike_ByReviewAuthor_ThrowsDomainException()
    {
        var author = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(author).BuildApproved();

        Should.Throw<DomainException>(() => review.AddLike(author));
    }

    [Fact]
    public void AddDislike_ByReviewAuthor_ThrowsDomainException()
    {
        var author = UserId.NewId();
        var review = new ProductReviewBuilder().WithUserId(author).BuildApproved();

        Should.Throw<DomainException>(() => review.AddDislike(author));
    }

    [Fact]
    public void AddLike_OnPendingReview_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).Build();

        Should.Throw<DomainException>(() => review.AddLike(UserId.NewId()));
    }

    [Fact]
    public void AddLike_OnRejectedReview_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildRejected();

        Should.Throw<DomainException>(() => review.AddLike(UserId.NewId()));
    }

    [Fact]
    public void AddLike_OnDeletedReview_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.MarkAsDeleted();

        Should.Throw<DomainException>(() => review.AddLike(UserId.NewId()));
    }

    [Fact]
    public void AddLike_WithNullUserId_ThrowsArgumentNullException()
    {
        var review = new ProductReviewBuilder().BuildApproved();

        Should.Throw<ArgumentNullException>(() => review.AddLike(null!));
    }

    [Fact]
    public void RemoveVote_WhenUserHasNotVoted_IsNoOp()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.RemoveVote(UserId.NewId());

        review.Votes.ShouldBeEmpty();
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveVote_WhenUserHasVoted_RemovesVoteAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        var voter = UserId.NewId();
        review.AddLike(voter);
        review.ClearDomainEvents();

        review.RemoveVote(voter);

        review.Votes.ShouldBeEmpty();
        review.LikeCount.ShouldBe(0);
        review.DislikeCount.ShouldBe(0);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewVoteChangedEvent>();
        evt.LikeCount.ShouldBe(0);
        evt.DislikeCount.ShouldBe(0);
    }

    [Fact]
    public void RemoveVote_OnDeletedReview_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        review.AddLike(UserId.NewId());
        review.MarkAsDeleted();

        Should.Throw<DomainException>(() => review.RemoveVote(UserId.NewId()));
    }

    [Fact]
    public void RemoveVote_WithNullUserId_ThrowsArgumentNullException()
    {
        var review = new ProductReviewBuilder().BuildApproved();

        Should.Throw<ArgumentNullException>(() => review.RemoveVote(null!));
    }

    [Fact]
    public void VoteCounts_WithMixedVotesFromMultipleUsers_AreComputedCorrectly()
    {
        var review = new ProductReviewBuilder().WithUserId(UserId.NewId()).BuildApproved();
        var u1 = UserId.NewId();
        var u2 = UserId.NewId();
        var u3 = UserId.NewId();
        var u4 = UserId.NewId();

        review.AddLike(u1);
        review.AddLike(u2);
        review.AddDislike(u3);
        review.AddLike(u4);
        review.AddDislike(u2);

        review.Votes.Count.ShouldBe(4);
        review.LikeCount.ShouldBe(2);
        review.DislikeCount.ShouldBe(2);
    }
}
