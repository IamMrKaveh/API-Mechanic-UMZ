using Presentation.Wishlist.Requests;

namespace Tests.Presentation.Wishlist.Requests;

public class WishlistRequestsTests
{
    [Fact]
    public void ToggleWishlistRequest_WithProductId_SetsCorrectly()
    {
        var productId = Guid.NewGuid();

        var request = new ToggleWishlistRequest(productId);

        request.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void ToggleWishlistRequest_IsRecord_EqualityWorks()
    {
        var productId = Guid.NewGuid();

        var request1 = new ToggleWishlistRequest(productId);
        var request2 = new ToggleWishlistRequest(productId);
        var request3 = new ToggleWishlistRequest(Guid.NewGuid());

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
