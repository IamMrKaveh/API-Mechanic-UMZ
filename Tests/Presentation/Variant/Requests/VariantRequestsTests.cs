using Presentation.Variant.Requests;

namespace Tests.Presentation.Variant.Requests;

public class VariantRequestsTests
{
    [Fact]
    public void AddVariantRequest_WithRequiredValues_SetsCorrectly()
    {
        var request = new AddVariantRequest("SKU1", 100, 120);

        request.Sku.ShouldBe("SKU1");
        request.SellingPrice.ShouldBe(100);
        request.OriginalPrice.ShouldBe(120);
        request.Stock.ShouldBe(0);
        request.IsUnlimited.ShouldBeFalse();
        request.ShippingMultiplier.ShouldBe(1);
        request.AttributeValueIds.ShouldBeNull();
        request.EnabledShippingIds.ShouldBeNull();
    }

    [Fact]
    public void AddVariantRequest_WithAllParameters_SetsCorrectly()
    {
        var attributeId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();

        var request = new AddVariantRequest("SKU1", 100, 120, 10, true, 1.5m, [attributeId], [shippingId]);

        request.Stock.ShouldBe(10);
        request.IsUnlimited.ShouldBeTrue();
        request.ShippingMultiplier.ShouldBe(1.5m);
        request.AttributeValueIds.ShouldContain(attributeId);
        request.EnabledShippingIds.ShouldContain(shippingId);
    }

    [Fact]
    public void UpdateVariantRequest_WithAllParameters_SetsCorrectly()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new UpdateVariantRequest(productId, variantId, userId, "SKU1", 100, 120, 10, false, 1, null, null);

        request.ProductId.ShouldBe(productId);
        request.VariantId.ShouldBe(variantId);
        request.UserId.ShouldBe(userId);
        request.Sku.ShouldBe("SKU1");
        request.SellingPrice.ShouldBe(100);
    }

    [Fact]
    public void AddStockRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new AddStockRequest(5, "restock");

        request.Quantity.ShouldBe(5);
        request.Notes.ShouldBe("restock");
    }

    [Fact]
    public void RemoveStockRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new RemoveStockRequest(2, "damage");

        request.Quantity.ShouldBe(2);
        request.Notes.ShouldBe("damage");
    }

    [Fact]
    public void UpdateVariantShippingRequest_WithAllParameters_SetsCorrectly()
    {
        var shippingId = Guid.NewGuid();

        var request = new UpdateVariantShippingRequest(1.5m, 500, [shippingId]);

        request.ShippingMultiplier.ShouldBe(1.5m);
        request.WeightGrams.ShouldBe(500);
        request.EnabledShippingIds.ShouldContain(shippingId);
    }

    [Fact]
    public void AddVariantRequest_IsRecord_EqualityWorks()
    {
        var request1 = new AddVariantRequest("SKU1", 100, 120);
        var request2 = new AddVariantRequest("SKU1", 100, 120);
        var request3 = new AddVariantRequest("SKU2", 100, 120);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
