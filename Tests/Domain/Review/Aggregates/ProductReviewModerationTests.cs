using Domain.Review.Events;
using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewModerationTests
{
    [Fact]
    public void Approve_OnPendingReview_TransitionsToApprovedAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().Build();
        review.ClearDomainEvents();
        var versionBefore = review.Version;

        review.Approve();

        review.Status.ShouldBe(ReviewStatus.Approved);
        review.RejectionReason.ShouldBeNull();
        review.UpdatedAt.ShouldNotBeNull();
        review.Version.ShouldBe(versionBefore + 1);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewApprovedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.Rating.ShouldBe(review.Rating);
    }

    [Fact]
    public void Approve_OnRejectedReview_ClearsRejectionReasonAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().BuildRejected("قبلاً رد شده");
        review.ClearDomainEvents();

        review.Approve();

        review.Status.ShouldBe(ReviewStatus.Approved);
        review.RejectionReason.ShouldBeNull();
        review.DomainEvents.ShouldContain(e => e is ReviewApprovedEvent);
    }

    [Fact]
    public void Approve_OnAlreadyApprovedReview_IsNoOp()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.Approve();

        review.Status.ShouldBe(ReviewStatus.Approved);
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reject_WithValidReason_TransitionsToRejectedAndStoresTrimmedReason()
    {
        var review = new ProductReviewBuilder().Build();
        review.ClearDomainEvents();

        review.Reject("  محتوای نامناسب  ");

        review.Status.ShouldBe(ReviewStatus.Rejected);
        review.RejectionReason.ShouldBe("محتوای نامناسب");
        review.UpdatedAt.ShouldNotBeNull();
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewRejectedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.Reason.ShouldBe("محتوای نامناسب");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithNullOrWhitespaceReason_ThrowsDomainException(string? reason)
    {
        var review = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => review.Reject(reason!));
    }

    [Fact]
    public void Reject_WithReasonAtMaxLength_Succeeds()
    {
        var review = new ProductReviewBuilder().Build();
        var reason = new string('د', 500);

        review.Reject(reason);

        review.RejectionReason.ShouldBe(reason);
    }

    [Fact]
    public void Reject_WithReasonOverMaxLength_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().Build();
        var reason = new string('د', 501);

        Should.Throw<DomainException>(() => review.Reject(reason));
    }

    [Fact]
    public void Reject_WithSameReasonOnAlreadyRejected_IsNoOp()
    {
        var review = new ProductReviewBuilder().BuildRejected("دلیل");
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.Reject("دلیل");

        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Reject_WithDifferentReasonOnAlreadyRejected_UpdatesReasonAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().BuildRejected("دلیل قدیمی");
        review.ClearDomainEvents();

        review.Reject("دلیل جدید");

        review.RejectionReason.ShouldBe("دلیل جدید");
        review.DomainEvents.ShouldContain(e => e is ReviewRejectedEvent);
    }
}
