using Application.Brand.Features.Shared;
using Application.Brand.Mapping;
using Mapster;

namespace Tests.Application.Brand.Mapping;

public class BrandProjectionConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public BrandProjectionConfigTests()
    {
        _config = new TypeAdapterConfig();
        new BrandProjectionConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public async Task Map_Brand_ToBrandListItemDto_MapsScalarsAndDefaultsEnrichedFields()
    {
        var brand = await new BrandBuilder()
            .WithName("Siemens")
            .WithSlug("siemens")
            .WithLogoPath("/logos/siemens.png")
            .BuildAsync();

        var dto = _mapper.Map<BrandListItemDto>(brand);

        dto.Id.ShouldBe(brand.Id.Value);
        dto.CategoryId.ShouldBe(brand.CategoryId.Value);
        dto.Name.ShouldBe("Siemens");
        dto.Slug.ShouldBe("siemens");
        dto.LogoPath.ShouldBe("/logos/siemens.png");
        dto.IsActive.ShouldBeTrue();
        dto.CategoryName.ShouldBe(string.Empty);
        dto.ProductCount.ShouldBe(0);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new BrandProjectionConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
