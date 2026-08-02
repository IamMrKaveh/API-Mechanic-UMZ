using Domain.Attribute.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Tests.Domain.Variant.ValueObjects;

public class AttributeAssignmentTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsAssignmentWithTrimmedDisplayValue()
    {
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();

        var sut = AttributeAssignment.Create(typeId, valueId, "  Red  ");

        sut.AttributeId.ShouldBe(typeId);
        sut.ValueId.ShouldBe(valueId);
        sut.DisplayValue.ShouldBe("Red");
    }

    [Fact]
    public void Create_WithNullAttributeId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => AttributeAssignment.Create(null!, AttributeValueId.NewId(), "Red"));
    }

    [Fact]
    public void Create_WithNullValueId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(
            () => AttributeAssignment.Create(AttributeTypeId.NewId(), null!, "Red"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceDisplayValue_ThrowsArgumentException(string? displayValue)
    {
        Should.Throw<ArgumentException>(
            () => AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), displayValue!));
    }

    [Fact]
    public void Equality_ForRecordWithSameMembers_TreatsInstancesAsEqual()
    {
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();

        var a = AttributeAssignment.Create(typeId, valueId, "Red");
        var b = AttributeAssignment.Create(typeId, valueId, "Red");

        a.ShouldBe(b);
    }

    [Fact]
    public void Equality_ForRecordWithDifferentDisplayValue_TreatsInstancesAsUnequal()
    {
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();

        var a = AttributeAssignment.Create(typeId, valueId, "Red");
        var b = AttributeAssignment.Create(typeId, valueId, "Rouge");

        a.ShouldNotBe(b);
    }
}
