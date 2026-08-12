using Domain.Review.Events;
using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewUpdateContentTests
{
    [Fact]
    public void UpdateContent_OnPendingReview_ReplacesFieldsAndSetsUpdatedAt()
    {
        var review = new ProductReviewBuilder().WithRating(3).Build();
        review.ClearDomainEvents();
        var newRating = Rating.Create(5);

        review.UpdateContent(newRating, "  عنوان جدید  ", "  متن جدید  ");

        review.Rating.ShouldBe(newRating);
        review.Title.ShouldBe("عنوان جدید");
        review.Comment.ShouldBe("متن جدید");
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.RejectionReason.ShouldBeNull();
        review.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateContent_OnPendingReview_RaisesReviewContentUpdatedEvent()
    {
        var review = new ProductReviewBuilder().Build();
        review.ClearDomainEvents();
        var newRating = Rating.Create(2);

        review.UpdateContent(newRating, "عنوان", "متن");

        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewContentUpdatedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.NewRating.ShouldBe(2);
    }

    [Fact]
    public void UpdateContent_OnRejectedReview_MovesBackToPendingAndClearsRejectionReason()
    {
        var review = new ProductReviewBuilder().BuildRejected("محتوای نامناسب");
        review.RejectionReason.ShouldBe("محتوای نامناسب");
        review.Status.ShouldBe(ReviewStatus.Rejected);

        review.UpdateContent(Rating.Create(4), "جدید", "متن");

        review.Status.ShouldBe(ReviewStatus.Pending);
        review.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void UpdateContent_OnApprovedReview_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().BuildApproved();

        Should.Throw<DomainException>(
            () => review.UpdateContent(Rating.Create(4), "x", "y"));
    }

    [Fact]
    public void UpdateContent_WithNullRating_ThrowsArgumentNullException()
    {
        var review = new ProductReviewBuilder().Build();

        Should.Throw<ArgumentNullException>(() => review.UpdateContent(null!, "x", "y"));
    }

    [Fact]
    public void UpdateContent_WithTitleAtMaxLength_Succeeds()
    {
        var review = new ProductReviewBuilder().Build();
        var title = new string('ا', 100);

        review.UpdateContent(Rating.Create(3), title, "متن");

        review.Title.ShouldBe(title);
    }

    [Fact]
    public void UpdateContent_WithTitleOverMaxLength_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().Build();
        var title = new string('ا', 101);

        Should.Throw<DomainException>(
            () => review.UpdateContent(Rating.Create(3), title, "متن"));
    }

    [Fact]
    public void UpdateContent_WithCommentOverMaxLength_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().Build();
        var comment = new string('م', 1001);

        Should.Throw<DomainException>(
            () => review.UpdateContent(Rating.Create(3), "ok", comment));
    }

    [Fact]
    public void UpdateContent_WithNullTitleAndComment_ClearsThem()
    {
        var review = new ProductReviewBuilder().WithTitle("old").WithComment("old").Build();

        review.UpdateContent(Rating.Create(3), null, null);

        review.Title.ShouldBeNull();
        review.Comment.ShouldBeNull();
    }
}
