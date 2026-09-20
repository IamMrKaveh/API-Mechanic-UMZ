using Application.Brand.Mapping;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Mapster;

namespace Tests.Application.Brand.Mapping;

public class BrandQueryMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public BrandQueryMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new BrandQueryMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_BrandId_ToGuid_ReturnsValue()
    {
        var id = BrandId.NewId();

        var guid = _mapper.Map<Guid>(id);

        guid.ShouldBe(id.Value);
    }

    [Fact]
    public void Map_CategoryId_ToGuid_ReturnsValue()
    {
        var id = CategoryId.NewId();

        var guid = _mapper.Map<Guid>(id);

        guid.ShouldBe(id.Value);
    }

    [Fact]
    public void Adapt_ViaConfigOverload_WorksForBothIds()
    {
        var brandId = BrandId.NewId();
        var categoryId = CategoryId.NewId();

        brandId.Adapt<Guid>(_config).ShouldBe(brandId.Value);
        categoryId.Adapt<Guid>(_config).ShouldBe(categoryId.Value);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new BrandQueryMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
