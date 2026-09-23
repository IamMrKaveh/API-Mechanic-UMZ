using Application.Variant.Features.Shared;
using Application.Variant.Mapping;
using Mapster;

namespace Tests.Application.Variant.Mapping;

public class VariantMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public VariantMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new VariantMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Variant_ToViewDto_MapsPricesDiscountAndActive()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(800m)
            .WithOriginalPrice(1000m)
            .Build();

        var dto = _mapper.Map<ProductVariantViewDto>(variant);

        dto.Id.ShouldBe(variant.Id.Value);
        dto.Sku.ShouldBe(variant.Sku.Value);
        dto.SellingPrice.ShouldBe(800m);
        dto.OriginalPrice.ShouldBe(1000m);
        dto.HasDiscount.ShouldBeTrue();
        dto.DiscountPercentage.ShouldBe(variant.DiscountPercentage ?? 0m);
        dto.IsActive.ShouldBe(variant.IsActive);
    }

    [Fact]
    public void Map_Variant_WithoutDiscount_MapsZeroPercentage()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(500m)
            .WithOriginalPrice(500m)
            .Build();

        var dto = _mapper.Map<ProductVariantViewDto>(variant);

        dto.HasDiscount.ShouldBeFalse();
        dto.DiscountPercentage.ShouldBe(0m);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new VariantMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
