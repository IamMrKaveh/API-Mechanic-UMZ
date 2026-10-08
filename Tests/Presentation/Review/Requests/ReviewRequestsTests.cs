using Presentation.Review.Requests;

namespace Tests.Presentation.Review.Requests;

public class ReviewRequestsTests
{
    [Fact]
    public void CreateReviewRequest_WithAllParameters_SetsCorrectly()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var request = new CreateReviewRequest(productId, orderId, 5, "Great", "Comment");

        request.ProductId.ShouldBe(productId);
        request.OrderId.ShouldBe(orderId);
        request.Rating.ShouldBe(5);
        request.Title.ShouldBe("Great");
        request.Comment.ShouldBe("Comment");
    }

    [Fact]
    public void CreateReviewRequest_WithNulls_SetsCorrectly()
    {
        var productId = Guid.NewGuid();

        var request = new CreateReviewRequest(productId, null, 4, null, null);

        request.OrderId.ShouldBeNull();
        request.Title.ShouldBeNull();
        request.Comment.ShouldBeNull();
    }

    [Fact]
    public void CreateReviewRequest_IsRecord_EqualityWorks()
    {
        var productId = Guid.NewGuid();

        var request1 = new CreateReviewRequest(productId, null, 5, null, null);
        var request2 = new CreateReviewRequest(productId, null, 5, null, null);
        var request3 = new CreateReviewRequest(productId, null, 1, null, null);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateOwnReviewRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateOwnReviewRequest(4, "Title", "Comment");

        request.Rating.ShouldBe(4);
        request.Title.ShouldBe("Title");
        request.Comment.ShouldBe("Comment");
    }

    [Fact]
    public void UpdateOwnReviewRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateOwnReviewRequest(4, "T", "C");
        var request2 = new UpdateOwnReviewRequest(4, "T", "C");
        var request3 = new UpdateOwnReviewRequest(5, "T", "C");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
