using Presentation.Review.Requests;

namespace Tests.Presentation.Review.Requests;

public class AdminReviewRequestsTests
{
    [Fact]
    public void ReplyToReviewRequest_WithReply_SetsCorrectly()
    {
        var request = new ReplyToReviewRequest("thanks");

        request.Reply.ShouldBe("thanks");
    }

    [Fact]
    public void RejectReviewRequest_WithReason_SetsCorrectly()
    {
        var request = new RejectReviewRequest("spam");

        request.Reason.ShouldBe("spam");
    }

    [Fact]
    public void UpdateReviewStatusRequest_WithStatusOnly_SetsReasonToNull()
    {
        var request = new UpdateReviewStatusRequest("Approved");

        request.Status.ShouldBe("Approved");
        request.Reason.ShouldBeNull();
    }

    [Fact]
    public void UpdateReviewStatusRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateReviewStatusRequest("Rejected", "spam");

        request.Status.ShouldBe("Rejected");
        request.Reason.ShouldBe("spam");
    }

    [Fact]
    public void BulkReviewActionRequest_WithIds_SetsCorrectly()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var request = new BulkReviewActionRequest(ids);

        request.ReviewIds.Count.ShouldBe(2);
    }

    [Fact]
    public void BulkRejectReviewsRequest_WithAllParameters_SetsCorrectly()
    {
        var ids = new List<Guid> { Guid.NewGuid() };

        var request = new BulkRejectReviewsRequest(ids, "spam");

        request.ReviewIds.ShouldBe(ids);
        request.Reason.ShouldBe("spam");
    }

    [Fact]
    public void BulkDeleteReviewsRequest_WithNullReason_SetsCorrectly()
    {
        var ids = new List<Guid> { Guid.NewGuid() };

        var request = new BulkDeleteReviewsRequest(ids);

        request.ReviewIds.ShouldBe(ids);
        request.Reason.ShouldBeNull();
    }

    [Fact]
    public void BulkDeleteReviewsRequest_IsRecord_EqualityWorks()
    {
        var ids = new List<Guid> { Guid.NewGuid() };

        var request1 = new BulkDeleteReviewsRequest(ids, "x");
        var request2 = new BulkDeleteReviewsRequest(ids, "x");
        var request3 = new BulkDeleteReviewsRequest(ids, "y");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
