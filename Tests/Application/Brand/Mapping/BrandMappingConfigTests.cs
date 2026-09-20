using Application.Brand.Features.Shared;
using Application.Brand.Mapping;
using Mapster;

namespace Tests.Application.Brand.Mapping;

public class BrandMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public BrandMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new BrandMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public async Task Map_Brand_ToBrandDetailDto_MapsValueObjects()
    {
        var brand = await new BrandBuilder()
            .WithName("Bosch")
            .WithSlug("bosch")
            .WithDescription("Tools")
            .WithLogoPath("/logos/bosch.png")
            .BuildAsync();

        var dto = _mapper.Map<BrandDetailDto>(brand);

        dto.Id.ShouldBe(brand.Id.Value);
        dto.Name.ShouldBe("Bosch");
        dto.Slug.ShouldBe("bosch");
        dto.Description.ShouldBe("Tools");
        dto.CategoryId.ShouldBe(brand.CategoryId.Value);
        dto.IsActive.ShouldBeTrue();
        dto.CreatedAt.ShouldBe(brand.CreatedAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new BrandMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
