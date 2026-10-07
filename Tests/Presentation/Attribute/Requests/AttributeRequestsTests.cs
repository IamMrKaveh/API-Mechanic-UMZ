using Presentation.Attribute.Requests;

namespace Tests.Presentation.Attribute.Requests;

public class AttributeRequestsTests
{
    [Fact]
    public void CreateAttributeTypeRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateAttributeTypeRequest("color", "Color", 1, true);

        request.Name.ShouldBe("color");
        request.DisplayName.ShouldBe("Color");
        request.SortOrder.ShouldBe(1);
        request.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void CreateAttributeTypeRequest_WithDefaultIsActive_SetsToTrue()
    {
        var request = new CreateAttributeTypeRequest("size", "Size", 0);

        request.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void CreateAttributeTypeRequest_WithFalseIsActive_SetsCorrectly()
    {
        var request = new CreateAttributeTypeRequest("color", "Color", 1, false);

        request.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void CreateAttributeTypeRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateAttributeTypeRequest("color", "Color", 1, true);
        var request2 = new CreateAttributeTypeRequest("color", "Color", 1, true);
        var request3 = new CreateAttributeTypeRequest("size", "Size", 1, true);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateAttributeTypeRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateAttributeTypeRequest("color", "Color", 2, false);

        request.Name.ShouldBe("color");
        request.DisplayName.ShouldBe("Color");
        request.SortOrder.ShouldBe(2);
        request.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void UpdateAttributeTypeRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateAttributeTypeRequest("color", "Color", 1, true);
        var request2 = new UpdateAttributeTypeRequest("color", "Color", 1, true);
        var request3 = new UpdateAttributeTypeRequest("color", "Color", 2, true);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void CreateAttributeValueRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateAttributeValueRequest("red", "Red", "#FF0000", 1);

        request.Value.ShouldBe("red");
        request.DisplayValue.ShouldBe("Red");
        request.HexCode.ShouldBe("#FF0000");
        request.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void CreateAttributeValueRequest_WithDefaultHexCode_SetsToNull()
    {
        var request = new CreateAttributeValueRequest("red", "Red");

        request.HexCode.ShouldBeNull();
    }

    [Fact]
    public void CreateAttributeValueRequest_WithDefaultSortOrder_SetsToZero()
    {
        var request = new CreateAttributeValueRequest("red", "Red");

        request.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void CreateAttributeValueRequest_WithNullHexCode_SetsToNull()
    {
        var request = new CreateAttributeValueRequest("red", "Red", null);

        request.HexCode.ShouldBeNull();
    }

    [Fact]
    public void CreateAttributeValueRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateAttributeValueRequest("red", "Red", "#FF0000", 1);
        var request2 = new CreateAttributeValueRequest("red", "Red", "#FF0000", 1);
        var request3 = new CreateAttributeValueRequest("blue", "Blue", "#0000FF", 1);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateAttributeValueRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateAttributeValueRequest("blue", "Blue", "#0000FF", 2, true);

        request.Value.ShouldBe("blue");
        request.DisplayValue.ShouldBe("Blue");
        request.HexCode.ShouldBe("#0000FF");
        request.SortOrder.ShouldBe(2);
        request.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void UpdateAttributeValueRequest_WithNullHexCode_SetsToNull()
    {
        var request = new UpdateAttributeValueRequest("blue", "Blue", null, 2, true);

        request.HexCode.ShouldBeNull();
    }

    [Fact]
    public void UpdateAttributeValueRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateAttributeValueRequest("red", "Red", "#FF0000", 1, true);
        var request2 = new UpdateAttributeValueRequest("red", "Red", "#FF0000", 1, true);
        var request3 = new UpdateAttributeValueRequest("red", "Red", "#0000FF", 1, true);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}