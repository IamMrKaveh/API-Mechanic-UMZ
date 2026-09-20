using Application.Category.Features.Shared;
using Application.Category.Mapping;
using Mapster;

namespace Tests.Application.Category.Mapping;

public class CategoryMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public CategoryMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new CategoryMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public async Task Map_Category_ToCategoryDto_MapsNameSlugDescriptionActive()
    {
        var category = await new CategoryBuilder()
            .WithName("Brakes")
            .WithSlug("brakes")
            .WithDescription("Brake parts")
            .WithSortOrder(4)
            .BuildAsync();

        var dto = _mapper.Map<CategoryDto>(category);

        dto.Id.ShouldBe(category.Id.Value);
        dto.Name.ShouldBe("Brakes");
        dto.Slug.ShouldBe("brakes");
        dto.Description.ShouldBe("Brake parts");
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new CategoryMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }

    [Fact]
    public async Task Map_ViaAdaptOverload_Works()
    {
        var category = await new CategoryBuilder().WithName("Engine").WithSlug("engine").BuildAsync();

        var dto = category.Adapt<CategoryDto>(_config);

        dto.Name.ShouldBe("Engine");
        dto.Id.ShouldBe(category.Id.Value);
    }
}
