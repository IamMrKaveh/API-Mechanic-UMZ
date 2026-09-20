using Application.Attribute.Features.Shared;
using Application.Attribute.Mapping;
using Domain.Attribute.Aggregates;
using Mapster;

namespace Tests.Application.Attribute.Mapping;

public class AttributeMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public AttributeMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new AttributeMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    private static async Task<AttributeType> CreateTypeWithValuesAsync()
    {
        var type = await new AttributeTypeBuilder().WithName("color").WithDisplayName("Color").WithSortOrder(3).BuildAsync();
        type.AddValue("red", "Red", DateTime.UtcNow, "#FF0000", 1);
        type.AddValue("blue", "Blue", DateTime.UtcNow, null, 2);
        return type;
    }

    [Fact]
    public async Task Map_AttributeType_ToDto_MapsScalarProperties()
    {
        var type = await new AttributeTypeBuilder().WithName("size").WithDisplayName("Size").WithSortOrder(7).BuildAsync();

        var dto = _mapper.Map<AttributeTypeDto>(type);

        dto.Id.ShouldBe(type.Id.Value);
        dto.Name.ShouldBe("size");
        dto.DisplayName.ShouldBe("Size");
        dto.SortOrder.ShouldBe(7);
        dto.IsActive.ShouldBe(type.IsActive);
    }

    [Fact]
    public async Task Map_AttributeType_ToDto_MapsValuesCollection()
    {
        var type = await CreateTypeWithValuesAsync();

        var dto = _mapper.Map<AttributeTypeDto>(type);

        dto.Values.Count.ShouldBe(2);
        dto.Values.ShouldContain(v => v.Value == "red" && v.HexCode == "#FF0000" && v.SortOrder == 1);
        dto.Values.ShouldContain(v => v.Value == "blue" && v.HexCode == null);
        foreach (var v in dto.Values)
            v.AttributeTypeId.ShouldBe(type.Id.Value);
    }

    [Fact]
    public async Task Map_AttributeValue_ToDto_MapsAllFields()
    {
        var type = await CreateTypeWithValuesAsync();
        var value = type.Values.First(v => v.Value == "red");

        var dto = _mapper.Map<AttributeValueDto>(value);

        dto.Id.ShouldBe(value.Id.Value);
        dto.AttributeTypeId.ShouldBe(type.Id.Value);
        dto.Value.ShouldBe("red");
        dto.DisplayValue.ShouldBe("Red");
        dto.HexCode.ShouldBe("#FF0000");
        dto.SortOrder.ShouldBe(1);
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Map_AttributeType_WithNoValues_MapsEmptyList()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();

        var dto = _mapper.Map<AttributeTypeDto>(type);

        dto.Values.ShouldNotBeNull();
        dto.Values.ShouldBeEmpty();
    }

    [Fact]
    public void Register_DoesNotThrow_AndProducesValidConfig()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new AttributeMappingConfig().Register(config));

        config.Compile();
    }

    [Fact]
    public async Task Map_ViaTypeAdapterConfig_AdaptOverload_Works()
    {
        var type = await new AttributeTypeBuilder().WithName("material").BuildAsync();

        var dto = type.Adapt<AttributeTypeDto>(_config);

        dto.Name.ShouldBe("material");
        dto.Id.ShouldBe(type.Id.Value);
    }
}
