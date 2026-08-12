using Domain.Review.Events;
using Domain.Review.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewAdminReplyTests
{
    [Fact]
    public void AddAdminReply_OnApprovedReview_SetsReplyAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.ClearDomainEvents();

        review.AddAdminReply("  ممنون از نظر شما  ");

        review.AdminReply.ShouldBe("ممنون از نظر شما");
        review.RepliedAt.ShouldNotBeNull();
        review.UpdatedAt.ShouldNotBeNull();
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewAdminRepliedEvent>();
        evt.ReviewId.ShouldBe(review.Id);
        evt.ProductId.ShouldBe(review.ProductId);
        evt.Reply.ShouldBe("ممنون از نظر شما");
    }

    [Fact]
    public void AddAdminReply_OnPendingReview_AlsoAutoApproves()
    {
        var review = new ProductReviewBuilder().Build();
        review.Status.ShouldBe(ReviewStatus.Pending);
        review.ClearDomainEvents();

        review.AddAdminReply("پاسخ");

        review.Status.ShouldBe(ReviewStatus.Approved);
        review.DomainEvents.Count.ShouldBe(2);
        review.DomainEvents.ElementAt(0).ShouldBeOfType<ReviewAdminRepliedEvent>();
        review.DomainEvents.ElementAt(1).ShouldBeOfType<ReviewApprovedEvent>();
    }

    [Fact]
    public void AddAdminReply_OnRejectedReview_DoesNotAutoApprove()
    {
        var review = new ProductReviewBuilder().BuildRejected();
        review.ClearDomainEvents();

        review.AddAdminReply("پاسخ به کاربر");

        review.Status.ShouldBe(ReviewStatus.Rejected);
        review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewAdminRepliedEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAdminReply_WithNullOrWhitespace_ThrowsDomainException(string? reply)
    {
        var review = new ProductReviewBuilder().BuildApproved();

        Should.Throw<DomainException>(() => review.AddAdminReply(reply!));
    }

    [Fact]
    public void AddAdminReply_WithReplyAtMaxLength_Succeeds()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        var reply = new string('پ', 1000);

        review.AddAdminReply(reply);

        review.AdminReply.ShouldBe(reply);
    }

    [Fact]
    public void AddAdminReply_WithReplyOverMaxLength_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        var reply = new string('پ', 1001);

        Should.Throw<DomainException>(() => review.AddAdminReply(reply));
    }

    [Fact]
    public void UpdateAdminReply_WithoutExistingReply_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().BuildApproved();

        Should.Throw<DomainException>(() => review.UpdateAdminReply("جدید"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateAdminReply_WithNullOrWhitespace_ThrowsDomainException(string? reply)
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddAdminReply("قدیمی");

        Should.Throw<DomainException>(() => review.UpdateAdminReply(reply!));
    }

    [Fact]
    public void UpdateAdminReply_WithReplyOverMaxLength_ThrowsDomainException()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddAdminReply("قدیمی");
        var reply = new string('پ', 1001);

        Should.Throw<DomainException>(() => review.UpdateAdminReply(reply));
    }

    [Fact]
    public void UpdateAdminReply_WithSameContent_IsNoOp()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddAdminReply("پاسخ");
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.UpdateAdminReply("پاسخ");

        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateAdminReply_WithDifferentContent_UpdatesReplyAndRaisesEvent()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddAdminReply("قدیمی");
        review.ClearDomainEvents();

        review.UpdateAdminReply("  پاسخ جدید  ");

        review.AdminReply.ShouldBe("پاسخ جدید");
        review.RepliedAt.ShouldNotBeNull();
        var evt = review.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<ReviewAdminRepliedEvent>();
        evt.Reply.ShouldBe("پاسخ جدید");
    }

    [Fact]
    public void RemoveAdminReply_WithoutExistingReply_IsNoOp()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.RemoveAdminReply();

        review.AdminReply.ShouldBeNull();
        review.RepliedAt.ShouldBeNull();
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveAdminReply_WithExistingReply_ClearsFieldsWithoutRaisingEvent()
    {
        var review = new ProductReviewBuilder().BuildApproved();
        review.AddAdminReply("پاسخ");
        var versionBefore = review.Version;
        review.ClearDomainEvents();

        review.RemoveAdminReply();

        review.AdminReply.ShouldBeNull();
        review.RepliedAt.ShouldBeNull();
        review.UpdatedAt.ShouldNotBeNull();
        review.Version.ShouldBe(versionBefore);
        review.DomainEvents.ShouldBeEmpty();
    }
}
