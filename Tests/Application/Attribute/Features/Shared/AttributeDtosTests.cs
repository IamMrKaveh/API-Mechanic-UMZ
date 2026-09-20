using Application.Attribute.Features.Shared;

namespace Tests.Application.Attribute.Features.Shared;

public class AttributeDtosTests
{
    [Fact]
    public void AttributeTypeDto_Defaults_AreEmpty()
    {
        var dto = new AttributeTypeDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Name.ShouldBe(string.Empty);
        dto.DisplayName.ShouldBe(string.Empty);
        dto.SortOrder.ShouldBe(0);
        dto.IsActive.ShouldBeFalse();
        dto.Values.ShouldNotBeNull();
        dto.Values.ShouldBeEmpty();
    }

    [Fact]
    public void AttributeTypeDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var dto = new AttributeTypeDto
        {
            Id = id,
            Name = "color",
            DisplayName = "Color",
            SortOrder = 2,
            IsActive = true,
            Values = new List<AttributeValueDto>
            {
                new() { Id = valueId, AttributeTypeId = id, Value = "red", DisplayValue = "Red", HexCode = "#FF0000", SortOrder = 1, IsActive = true }
            }
        };

        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("color");
        dto.DisplayName.ShouldBe("Color");
        dto.Values.Count.ShouldBe(1);
        dto.Values[0].HexCode.ShouldBe("#FF0000");
    }

    [Fact]
    public void AttributeValueDto_ValueDefault_IsNull()
    {
        var dto = new AttributeValueDto();

        dto.Value.ShouldBeNull();
        dto.DisplayValue.ShouldBe(string.Empty);
        dto.HexCode.ShouldBeNull();
        dto.SortOrder.ShouldBe(0);
        dto.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void AttributeValueDto_NullableHexCode_AcceptsValueAndNull()
    {
        var dto = new AttributeValueDto { Id = Guid.NewGuid(), Value = "blue", DisplayValue = "Blue", HexCode = "#0000FF" };
        dto.HexCode.ShouldBe("#0000FF");

        var withoutHex = dto with { HexCode = null };
        withoutHex.HexCode.ShouldBeNull();
        withoutHex.Value.ShouldBe("blue");
    }

    [Fact]
    public void UpdateAttributeTypeDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateAttributeTypeDto
        {
            Id = id,
            Name = "size",
            DisplayName = "Size",
            SortOrder = 5,
            IsActive = false
        };

        dto.Id.ShouldBe(id);
        dto.Name.ShouldBe("size");
        dto.DisplayName.ShouldBe("Size");
        dto.SortOrder.ShouldBe(5);
        dto.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void AttributeTypeDto_ValuesList_IsMutableIndependently()
    {
        var dto = new AttributeTypeDto();
        dto.Values.Add(new AttributeValueDto { Value = "x", DisplayValue = "X" });

        dto.Values.Count.ShouldBe(1);

        var other = new AttributeTypeDto();
        other.Values.ShouldBeEmpty();
    }

    [Fact]
    public void DtoRecords_ValueEquality_Works()
    {
        var id = Guid.NewGuid();
        var a = new UpdateAttributeTypeDto { Id = id, Name = "n", DisplayName = "d", SortOrder = 1, IsActive = true };
        var b = new UpdateAttributeTypeDto { Id = id, Name = "n", DisplayName = "d", SortOrder = 1, IsActive = true };

        a.ShouldBe(b);
    }
}
