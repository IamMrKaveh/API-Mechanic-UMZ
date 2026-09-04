using Domain.Product.ValueObjects;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Review.Events;

public class ReviewEventsTests
{
    [Fact]
    public void ReviewSubmittedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var reviewId = ReviewId.NewId();
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        var rating = Rating.Create(5);

        var sut = new ReviewSubmittedEvent(reviewId, productId, userId, rating);

        sut.ReviewId.ShouldBe(reviewId);
        sut.ProductId.ShouldBe(productId);
        sut.UserId.ShouldBe(userId);
        sut.Rating.ShouldBe(rating);
    }

    [Fact]
    public void ReviewApprovedEvent_ExposesRating()
    {
        var rating = Rating.Create(4);

        var sut = new ReviewApprovedEvent(ReviewId.NewId(), ProductId.NewId(), rating);

        sut.Rating.Value.ShouldBe(4);
    }

    [Fact]
    public void ReviewRejectedEvent_WithReason_StoresIt()
    {
        var sut = new ReviewRejectedEvent(ReviewId.NewId(), ProductId.NewId(), "Spam");

        sut.Reason.ShouldBe("Spam");
    }

    [Fact]
    public void ReviewRejectedEvent_WithoutReason_StoresNull()
    {
        var sut = new ReviewRejectedEvent(ReviewId.NewId(), ProductId.NewId(), null);

        sut.Reason.ShouldBeNull();
    }

    [Fact]
    public void ReviewAdminRepliedEvent_StoresReply()
    {
        var sut = new ReviewAdminRepliedEvent(ReviewId.NewId(), ProductId.NewId(), "Thanks!");

        sut.Reply.ShouldBe("Thanks!");
    }

    [Fact]
    public void ReviewContentUpdatedEvent_StoresNewRating()
    {
        var sut = new ReviewContentUpdatedEvent(ReviewId.NewId(), ProductId.NewId(), 3);

        sut.NewRating.ShouldBe(3);
    }

    [Fact]
    public void ReviewDeletedEvent_StoresUserId()
    {
        var userId = UserId.NewId();

        var sut = new ReviewDeletedEvent(ReviewId.NewId(), ProductId.NewId(), userId);

        sut.UserId.ShouldBe(userId);
    }

    [Fact]
    public void ReviewRestoredEvent_ExposesIds()
    {
        var reviewId = ReviewId.NewId();
        var productId = ProductId.NewId();

        var sut = new ReviewRestoredEvent(reviewId, productId);

        sut.ReviewId.ShouldBe(reviewId);
        sut.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void ReviewVoteChangedEvent_StoresCounts()
    {
        var sut = new ReviewVoteChangedEvent(ReviewId.NewId(), likeCount: 7, dislikeCount: 2);

        sut.LikeCount.ShouldBe(7);
        sut.DislikeCount.ShouldBe(2);
    }
}
