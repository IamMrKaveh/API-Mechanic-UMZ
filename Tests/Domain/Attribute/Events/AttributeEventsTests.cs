using Domain.Attribute.Events;
using Domain.Attribute.ValueObjects;

namespace Tests.Domain.Attribute.Events;

public class AttributeEventsTests
{
    [Fact]
    public void AttributeTypeCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var id = AttributeTypeId.NewId();

        var sut = new AttributeTypeCreatedEvent(id, "color", "Color", 5);

        sut.AttributeTypeId.ShouldBe(id);
        sut.Name.ShouldBe("color");
        sut.DisplayName.ShouldBe("Color");
        sut.SortOrder.ShouldBe(5);
    }

    [Fact]
    public void AttributeValueAddedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();

        var sut = new AttributeValueAddedEvent(typeId, valueId, "red", "Red");

        sut.AttributeTypeId.ShouldBe(typeId);
        sut.AttributeValueId.ShouldBe(valueId);
        sut.Value.ShouldBe("red");
        sut.DisplayValue.ShouldBe("Red");
    }
}
