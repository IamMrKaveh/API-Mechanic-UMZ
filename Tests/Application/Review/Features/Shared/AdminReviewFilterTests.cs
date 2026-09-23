using Application.Review.Features.Shared;

namespace Tests.Application.Review.Features.Shared;

public class AdminReviewFilterTests
{
    [Fact]
    public void Constructor_StoresAllPositions()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 2, 1);
        var productId = Guid.NewGuid();

        var filter = new AdminReviewFilter("Pending", 2, 25, "brake", 4, productId, from, to);

        filter.Status.ShouldBe("Pending");
        filter.Page.ShouldBe(2);
        filter.PageSize.ShouldBe(25);
        filter.SearchText.ShouldBe("brake");
        filter.MinRating.ShouldBe(4);
        filter.ProductId.ShouldBe(productId);
        filter.DateFrom.ShouldBe(from);
        filter.DateTo.ShouldBe(to);
    }

    [Fact]
    public void Constructor_AllowsNullOptionals()
    {
        var filter = new AdminReviewFilter("Approved", 1, 10, null, null, null, null, null);

        filter.SearchText.ShouldBeNull();
        filter.MinRating.ShouldBeNull();
        filter.ProductId.ShouldBeNull();
        filter.DateFrom.ShouldBeNull();
        filter.DateTo.ShouldBeNull();
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        var a = new AdminReviewFilter("S", 1, 10, "x", 5, Guid.NewGuid(), null, null);
        var b = a with { };

        a.ShouldBe(b);
    }

    [Fact]
    public void Deconstruct_ReturnsAllPositions()
    {
        var filter = new AdminReviewFilter("Rejected", 3, 15, null, 1, null, null, null);

        var (status, page, pageSize, _, minRating, _, _, _) = filter;

        status.ShouldBe("Rejected");
        page.ShouldBe(3);
        pageSize.ShouldBe(15);
        minRating.ShouldBe(1);
    }
}
