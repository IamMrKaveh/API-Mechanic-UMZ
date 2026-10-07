using Presentation.Cart.Requests;

namespace Tests.Presentation.Cart.Requests;

public class CartRequestsTests
{
    [Fact]
    public void AddCartItemRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var request = new AddCartItemRequest(variantId, 2);

        request.VariantId.ShouldBe(variantId);
        request.Quantity.ShouldBe(2);
    }

    [Fact]
    public void AddCartItemRequest_IsRecord_EqualityWorks()
    {
        var variantId = Guid.NewGuid();

        var request1 = new AddCartItemRequest(variantId, 2);
        var request2 = new AddCartItemRequest(variantId, 2);
        var request3 = new AddCartItemRequest(variantId, 3);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateCartItemQuantityRequest_WithQuantity_SetsCorrectly()
    {
        var request = new UpdateCartItemQuantityRequest(5);

        request.Quantity.ShouldBe(5);
    }

    [Fact]
    public void UpdateCartItemQuantityRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateCartItemQuantityRequest(5);
        var request2 = new UpdateCartItemQuantityRequest(5);
        var request3 = new UpdateCartItemQuantityRequest(1);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
