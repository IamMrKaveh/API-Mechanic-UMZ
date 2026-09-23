using Application.Product.Features.Shared;
using Application.Product.Mapping;
using Mapster;

namespace Tests.Application.Product.Mapping;

public class ProductMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public ProductMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new ProductMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Product_ToProductDetailDto_MapsScalarsAndIgnoresEnriched()
    {
        var product = new ProductBuilder()
            .WithName("Brake Pad")
            .WithSlug("brake-pad")
            .WithDescription("Front pads")
            .Build();

        var dto = _mapper.Map<ProductDetailDto>(product);

        dto.Id.ShouldBe(product.Id.Value);
        dto.Name.ShouldBe("Brake Pad");
        dto.Slug.ShouldBe("brake-pad");
        dto.Description.ShouldBe("Front pads");
        dto.BrandId.ShouldBe(product.BrandId.Value);
        dto.CategoryId.ShouldBe(product.CategoryId.Value);
        dto.IsActive.ShouldBe(product.IsActive);
        dto.IsFeatured.ShouldBe(product.IsFeatured);
        dto.CreatedAt.ShouldBe(product.CreatedAt);
        dto.UpdatedAt.ShouldBe(product.UpdatedAt);
        dto.CategoryName.ShouldBe(string.Empty);
        dto.BrandName.ShouldBe(string.Empty);
        dto.Variants.ShouldBeEmpty();
    }

    [Fact]
    public void Map_Product_ToProductListItemDto_MapsIdentityFlags()
    {
        var product = new ProductBuilder().WithName("Oil Filter").Build();

        var dto = _mapper.Map<ProductListItemDto>(product);

        dto.Id.ShouldBe(product.Id.Value);
        dto.Name.ShouldBe(product.Name.Value);
        dto.Slug.ShouldBe(product.Slug.Value);
        dto.BrandId.ShouldBe(product.BrandId.Value);
        dto.IsActive.ShouldBe(product.IsActive);
        dto.IsFeatured.ShouldBe(product.IsFeatured);
        dto.IsDeleted.ShouldBe(product.IsDeleted);
        dto.CreatedAt.ShouldBe(product.CreatedAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new ProductMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
