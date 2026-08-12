using Domain.Review.Events;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewSoftDeleteTests
{
    [Fact]
    public void MarkAsDeleted_OnActiveReview_SetsIsDeletedAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().Build();
        review.ClearDomainEvents();

        review.MarkAsDeleted();

        review.IsDeleted.ShouldBeTrue();
        review.UpdatedAt.ShouldNotBeNull();
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewDeletedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.UserId.ShouldBe(review.UserId);
    }

    [Fact]
    public void MarkAsDeleted_OnAlreadyDeletedReview_IsNoOp()
    {
        var review = new ProductReviewBuilder().BuildDeleted();
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.MarkAsDeleted();

        review.IsDeleted.ShouldBeTrue();
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_OnDeletedReview_ClearsIsDeletedAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().BuildDeleted();
        review.ClearDomainEvents();

        review.Restore();

        review.IsDeleted.ShouldBeFalse();
        review.UpdatedAt.ShouldNotBeNull();
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewRestoredEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
    }

    [Fact]
    public void Restore_OnActiveReview_IsNoOp()
    {
        var review = new ProductReviewBuilder().Build();
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.Restore();

        review.IsDeleted.ShouldBeFalse();
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }
}
