using Domain.Review.Entities;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Review.Aggregates;

public class ProductReviewTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedProductReview()
    {
        var sut = new ProductReviewBuilder()
            .WithTitle("عالی")
            .WithComment("توضیحات کامل")
            .WithRating(5)
            .WithVerifiedPurchase(true)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Title.ShouldBe("عالی");
        sut.Comment.ShouldBe("توضیحات کامل");
        sut.Rating.Value.ShouldBe(5);
        sut.IsVerifiedPurchase.ShouldBeTrue();
        sut.Status.ShouldBe(ReviewStatus.Pending);
        sut.LikeCount.ShouldBe(0);
        sut.DislikeCount.ShouldBe(0);
        sut.AdminReply.ShouldBeNull();
        sut.RepliedAt.ShouldBeNull();
        sut.IsDeleted.ShouldBeFalse();
        sut.UpdatedAt.ShouldBeNull();
        sut.Votes.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new ProductReviewBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_TrimsTitleAndComment()
    {
        var sut = new ProductReviewBuilder()
            .WithTitle("  عنوان  ")
            .WithComment("  متن نظر  ")
            .Build();

        sut.Title.ShouldBe("عنوان");
        sut.Comment.ShouldBe("متن نظر");
    }

    [Fact]
    public void Create_WithNullTitleAndComment_KeepsThemNull()
    {
        var sut = new ProductReviewBuilder().WithTitle(null).WithComment(null).Build();

        sut.Title.ShouldBeNull();
        sut.Comment.ShouldBeNull();
    }

    [Fact]
    public void Create_WithTitleOver100Characters_ThrowsDomainException()
    {
        var longTitle = new string('a', 101);

        Should.Throw<DomainException>(() => new ProductReviewBuilder().WithTitle(longTitle).Build());
    }

    [Fact]
    public void Create_WithCommentOver1000Characters_ThrowsDomainException()
    {
        var longComment = new string('a', 1001);

        Should.Throw<DomainException>(() => new ProductReviewBuilder().WithComment(longComment).Build());
    }

    [Fact]
    public void Create_RaisesExactlyOneReviewSubmittedEvent()
    {
        var sut = new ProductReviewBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ReviewSubmittedEvent>();
        evt.ReviewId.ShouldBe(sut.Id);
        evt.ProductId.ShouldBe(sut.ProductId);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Rating.ShouldBe(sut.Rating);
    }

    [Fact]
    public void UpdateContent_OnPendingReview_UpdatesFieldsAndKeepsPending()
    {
        var sut = new ProductReviewBuilder().Build();
        var newRating = Rating.Create(3);

        sut.UpdateContent(newRating, "  عنوان جدید  ", "  متن جدید  ");

        sut.Rating.ShouldBe(newRating);
        sut.Title.ShouldBe("عنوان جدید");
        sut.Comment.ShouldBe("متن جدید");
        sut.Status.ShouldBe(ReviewStatus.Pending);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateContent_OnRejectedReview_ClearsRejectionReasonAndReturnsToPending()
    {
        var sut = new ProductReviewBuilder().BuildRejected("موقتی");

        sut.UpdateContent(Rating.Create(2), "t", "c");

        sut.Status.ShouldBe(ReviewStatus.Pending);
        sut.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void UpdateContent_OnApprovedReview_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().BuildApproved();

        Should.Throw<DomainException>(() => sut.UpdateContent(Rating.Create(1), "t", "c"));
    }

    [Fact]
    public void UpdateContent_RaisesReviewContentUpdatedEvent()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.UpdateContent(Rating.Create(2), "t", "c");

        var evt = sut.DomainEvents.Single().ShouldBeOfType<ReviewContentUpdatedEvent>();
        evt.ReviewId.ShouldBe(sut.Id);
        evt.ProductId.ShouldBe(sut.ProductId);
    }

    [Fact]
    public void UpdateContent_WithTitleTooLong_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.UpdateContent(Rating.Create(1), new string('a', 101), "c"));
    }

    [Fact]
    public void UpdateContent_WithCommentTooLong_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.UpdateContent(Rating.Create(1), "t", new string('a', 1001)));
    }

    [Fact]
    public void Approve_OnPendingReview_TransitionsToApprovedAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.Approve();

        sut.Status.ShouldBe(ReviewStatus.Approved);
        sut.RejectionReason.ShouldBeNull();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.DomainEvents.Single().ShouldBeOfType<ReviewApprovedEvent>();
    }

    [Fact]
    public void Approve_OnAlreadyApprovedReview_IsNoOp()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        sut.ClearDomainEvents();

        sut.Approve();

        sut.Status.ShouldBe(ReviewStatus.Approved);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Approve_OnRejectedReview_ClearsRejectionReasonAndApproves()
    {
        var sut = new ProductReviewBuilder().BuildRejected("زشت");
        sut.ClearDomainEvents();

        sut.Approve();

        sut.Status.ShouldBe(ReviewStatus.Approved);
        sut.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void Reject_WithReason_TransitionsToRejectedAndCapturesTrimmedReason()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.Reject("  دلیل رد  ");

        sut.Status.ShouldBe(ReviewStatus.Rejected);
        sut.RejectionReason.ShouldBe("دلیل رد");
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ReviewRejectedEvent>();
        evt.Reason.ShouldBe("دلیل رد");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithNullOrWhitespaceReason_ThrowsDomainException(string? reason)
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.Reject(reason!));
    }

    [Fact]
    public void Reject_WithReasonOver500Characters_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.Reject(new string('a', 501)));
    }

    [Fact]
    public void Reject_WithSameReasonOnRejectedReview_IsNoOp()
    {
        var sut = new ProductReviewBuilder().BuildRejected("دلیل");
        sut.ClearDomainEvents();

        sut.Reject("دلیل");

        sut.RejectionReason.ShouldBe("دلیل");
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddAdminReply_OnPendingReview_SetsReplyAndApprovesReview()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.AddAdminReply("  پاسخ ادمین  ");

        sut.AdminReply.ShouldBe("پاسخ ادمین");
        sut.RepliedAt.ShouldNotBeNull();
        sut.Status.ShouldBe(ReviewStatus.Approved);
        sut.DomainEvents.OfType<ReviewAdminRepliedEvent>().ShouldHaveSingleItem();
        sut.DomainEvents.OfType<ReviewApprovedEvent>().ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAdminReply_WithNullOrWhitespaceReply_ThrowsDomainException(string? reply)
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.AddAdminReply(reply!));
    }

    [Fact]
    public void AddAdminReply_WithReplyOver1000Characters_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.AddAdminReply(new string('a', 1001)));
    }

    [Fact]
    public void UpdateAdminReply_WithoutExistingReply_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.UpdateAdminReply("new"));
    }

    [Fact]
    public void UpdateAdminReply_WithChangedText_UpdatesReplyAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.AddAdminReply("پاسخ اول");
        sut.ClearDomainEvents();

        sut.UpdateAdminReply("  پاسخ دوم  ");

        sut.AdminReply.ShouldBe("پاسخ دوم");
        var evt = sut.DomainEvents.Single().ShouldBeOfType<ReviewAdminRepliedEvent>();
        evt.Reply.ShouldBe("پاسخ دوم");
    }

    [Fact]
    public void UpdateAdminReply_WithSameText_IsNoOp()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.AddAdminReply("پاسخ");
        sut.ClearDomainEvents();

        sut.UpdateAdminReply("پاسخ");

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveAdminReply_WhenReplyExists_ClearsReplyAndReliedAt()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.AddAdminReply("پاسخ");

        sut.RemoveAdminReply();

        sut.AdminReply.ShouldBeNull();
        sut.RepliedAt.ShouldBeNull();
    }

    [Fact]
    public void RemoveAdminReply_WhenNoReply_IsNoOp()
    {
        var sut = new ProductReviewBuilder().Build();

        sut.RemoveAdminReply();

        sut.AdminReply.ShouldBeNull();
    }

    [Fact]
    public void MarkAsDeleted_WhenNotDeleted_TogglesFlagAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.MarkAsDeleted();

        sut.IsDeleted.ShouldBeTrue();
        sut.DomainEvents.Single().ShouldBeOfType<ReviewDeletedEvent>();
    }

    [Fact]
    public void MarkAsDeleted_WhenAlreadyDeleted_IsNoOp()
    {
        var sut = new ProductReviewBuilder().BuildDeleted();
        sut.ClearDomainEvents();

        sut.MarkAsDeleted();

        sut.IsDeleted.ShouldBeTrue();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_WhenDeleted_ClearsDeletedFlagAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().BuildDeleted();
        sut.ClearDomainEvents();

        sut.Restore();

        sut.IsDeleted.ShouldBeFalse();
        sut.DomainEvents.Single().ShouldBeOfType<ReviewRestoredEvent>();
    }

    [Fact]
    public void Restore_WhenNotDeleted_IsNoOp()
    {
        var sut = new ProductReviewBuilder().Build();
        sut.ClearDomainEvents();

        sut.Restore();

        sut.IsDeleted.ShouldBeFalse();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddLike_OnApprovedReviewByOtherUser_IncrementsLikeCountAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        sut.ClearDomainEvents();
        var voter = UserId.NewId();

        sut.AddLike(voter);

        sut.LikeCount.ShouldBe(1);
        sut.DislikeCount.ShouldBe(0);
        sut.Votes.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ReviewVoteChangedEvent>();
    }

    [Fact]
    public void AddDislike_OnApprovedReview_IncrementsDislikeCount()
    {
        var sut = new ProductReviewBuilder().BuildApproved();

        sut.AddDislike(UserId.NewId());

        sut.DislikeCount.ShouldBe(1);
        sut.LikeCount.ShouldBe(0);
    }

    [Fact]
    public void AddLike_TwiceBySameUser_IsIdempotent()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        var voter = UserId.NewId();
        sut.AddLike(voter);
        sut.ClearDomainEvents();

        sut.AddLike(voter);

        sut.LikeCount.ShouldBe(1);
        sut.Votes.Count.ShouldBe(1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AddDislike_ByUserWhoAlreadyLiked_ChangesVoteType()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        var voter = UserId.NewId();
        sut.AddLike(voter);

        sut.AddDislike(voter);

        sut.LikeCount.ShouldBe(0);
        sut.DislikeCount.ShouldBe(1);
        sut.Votes.Count.ShouldBe(1);
    }

    [Fact]
    public void AddLike_OnDeletedReview_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        sut.MarkAsDeleted();

        Should.Throw<DomainException>(() => sut.AddLike(UserId.NewId()));
    }

    [Fact]
    public void AddLike_OnUnapprovedReview_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().Build();

        Should.Throw<DomainException>(() => sut.AddLike(UserId.NewId()));
    }

    [Fact]
    public void AddLike_ByReviewAuthor_ThrowsDomainException()
    {
        var author = UserId.NewId();
        var sut = new ProductReviewBuilder().WithUserId(author).BuildApproved();

        Should.Throw<DomainException>(() => sut.AddLike(author));
    }

    [Fact]
    public void RemoveVote_ByExistingVoter_DecrementsCountAndRaisesEvent()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        var voter = UserId.NewId();
        sut.AddLike(voter);
        sut.ClearDomainEvents();

        sut.RemoveVote(voter);

        sut.LikeCount.ShouldBe(0);
        sut.Votes.ShouldBeEmpty();
        sut.DomainEvents.Single().ShouldBeOfType<ReviewVoteChangedEvent>();
    }

    [Fact]
    public void RemoveVote_ByNonVoter_IsNoOp()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        sut.ClearDomainEvents();

        sut.RemoveVote(UserId.NewId());

        sut.Votes.ShouldBeEmpty();
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveVote_OnDeletedReview_ThrowsDomainException()
    {
        var sut = new ProductReviewBuilder().BuildApproved();
        var voter = UserId.NewId();
        sut.AddLike(voter);
        sut.MarkAsDeleted();

        Should.Throw<DomainException>(() => sut.RemoveVote(voter));
    }

    [Fact]
    public void Votes_ExposesReadOnlyCollectionOfReviewVote()
    {
        var sut = new ProductReviewBuilder().Build();

        sut.Votes.ShouldBeAssignableTo<IReadOnlyCollection<ReviewVote>>();
    }
}
