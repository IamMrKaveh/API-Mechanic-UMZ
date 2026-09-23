using Application.Shipping.Features.Shared;
using Application.Shipping.Mapping;
using Mapster;

namespace Tests.Application.Shipping.Mapping;

public class ShippingMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public ShippingMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new ShippingMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Shipping_ToShippingDto_MapsCostsRangesAndThresholds()
    {
        var shipping = new ShippingBuilder()
            .WithName("Express")
            .WithBaseCost(80_000m)
            .WithDescription("Fast delivery")
            .WithDeliveryDays(1, 2)
            .Build();

        var dto = _mapper.Map<ShippingDto>(shipping);

        dto.Id.ShouldBe(shipping.Id.Value);
        dto.Name.ShouldBe("Express");
        dto.Description.ShouldBe("Fast delivery");
        dto.BaseCost.ShouldBe(80_000m);
        dto.EstimatedDeliveryTime.ShouldBe(shipping.EstimatedDeliveryTime);
        dto.MinDeliveryDays.ShouldBe(1);
        dto.MaxDeliveryDays.ShouldBe(2);
        dto.IsActive.ShouldBe(shipping.IsActive);
        dto.IsDefault.ShouldBe(shipping.IsDefault);
        dto.SortOrder.ShouldBe(shipping.SortOrder);
        dto.CreatedAt.ShouldBe(shipping.CreatedAt);
        dto.UpdatedAt.ShouldBe(shipping.UpdatedAt);
    }

    [Fact]
    public void Map_Shipping_ToShippingListItemDto_MapsDisplay()
    {
        var shipping = new ShippingBuilder().WithName("Standard").WithDeliveryDays(2, 5).Build();

        var dto = _mapper.Map<ShippingListItemDto>(shipping);

        dto.Id.ShouldBe(shipping.Id.Value);
        dto.Name.ShouldBe("Standard");
        dto.BaseCost.ShouldBe(shipping.BaseCost.Amount);
        dto.DeliveryTimeDisplay.ShouldBe(shipping.GetDeliveryTimeDisplay());
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new ShippingMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
