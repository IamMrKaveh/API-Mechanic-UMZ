using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewCreationTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedReview()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        var rating = new RatingBuilder().WithValue(5).Build();

        var review = new ProductReviewBuilder()
            .WithProductId(productId)
            .WithUserId(userId)
            .WithRating(rating)
            .WithTitle("عالی")
            .WithComment("محصول خوبی بود")
            .WithVerifiedPurchase(true)
            .Build();

        review.ShouldNotBeNull();
        review.Id.ShouldNotBeNull();
        review.Id.Value.ShouldNotBe(Guid.Empty);
        review.ProductId.ShouldBe(productId);
        review.UserId.ShouldBe(userId);
        review.Rating.ShouldBe(rating);
        review.Title.ShouldBe("عالی");
        review.Comment.ShouldBe("محصول خوبی بود");
        review.IsVerifiedPurchase.ShouldBeTrue();
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.LikeCount.ShouldBe(0);
        review.DislikeCount.ShouldBe(0);
        review.AdminReply.ShouldBeNull();
        review.RepliedAt.ShouldBeNull();
        review.RejectionReason.ShouldBeNull();
        review.IsDeleted.ShouldBeFalse();
        review.UpdatedAt.ShouldBeNull();
        review.Votes.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var review = new ProductReviewBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        review.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        review.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithoutOrderId_LeavesOrderIdNull()
    {
        var review = new ProductReviewBuilder().WithoutOrderId().Build();

        review.OrderId.ShouldBeNull();
    }

    [Fact]
    public void Create_WithOrderId_AssignsIt()
    {
        var orderId = OrderId.NewId();

        var review = new ProductReviewBuilder().WithOrderId(orderId).Build();

        review.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void Create_ProducesReviewWithVersionOne()
    {
        var review = new ProductReviewBuilder().Build();

        review.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneReviewSubmittedEvent()
    {
        var review = new ProductReviewBuilder().Build();

        review.DomainEvents.Count.ShouldBe(1);
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewSubmittedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.UserId.ShouldBe(review.UserId);
        evt.Rating.ShouldBe(review.Rating);
    }

    [Fact]
    public void Create_WithNullProductId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ProductReview.Create(null!, UserId.NewId(), Rating.Create(3), null, null, false));
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ProductReview.Create(ProductId.NewId(), null!, Rating.Create(3), null, null, false));
    }

    [Fact]
    public void Create_WithNullRating_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ProductReview.Create(ProductId.NewId(), UserId.NewId(), null!, null, null, false));
    }

    [Fact]
    public void Create_WithTitleAtMaxLength_Succeeds()
    {
        var title = new string('ا', 100);

        var review = new ProductReviewBuilder().WithTitle(title).Build();

        review.Title.ShouldBe(title);
    }

    [Fact]
    public void Create_WithTitleOverMaxLength_ThrowsDomainException()
    {
        var title = new string('ا', 101);

        Should.Throw<DomainException>(() => new ProductReviewBuilder().WithTitle(title).Build());
    }

    [Fact]
    public void Create_WithCommentAtMaxLength_Succeeds()
    {
        var comment = new string('م', 1000);

        var review = new ProductReviewBuilder().WithComment(comment).Build();

        review.Comment.ShouldBe(comment);
    }

    [Fact]
    public void Create_WithCommentOverMaxLength_ThrowsDomainException()
    {
        var comment = new string('م', 1001);

        Should.Throw<DomainException>(() => new ProductReviewBuilder().WithComment(comment).Build());
    }

    [Fact]
    public void Create_TrimsTitleAndComment()
    {
        var review = new ProductReviewBuilder()
            .WithTitle("  عنوان  ")
            .WithComment("  متن  ")
            .Build();

        review.Title.ShouldBe("عنوان");
        review.Comment.ShouldBe("متن");
    }

    [Fact]
    public void Create_WithNullTitleAndComment_KeepsThemNull()
    {
        var review = new ProductReviewBuilder().WithTitle(null).WithComment(null).Build();

        review.Title.ShouldBeNull();
        review.Comment.ShouldBeNull();
    }

    [Fact]
    public void Create_WithVerifiedPurchaseFalse_PropagatesFlag()
    {
        var review = new ProductReviewBuilder().WithVerifiedPurchase(false).Build();

        review.IsVerifiedPurchase.ShouldBeFalse();
    }
}
