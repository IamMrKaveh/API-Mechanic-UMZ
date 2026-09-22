using Application.Discount.Features.Shared;
using Application.Discount.Mapping;
using Domain.Discount.ValueObjects;
using Mapster;

namespace Tests.Application.Discount.Mapping;

public class DiscountMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public DiscountMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new DiscountMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_DiscountCode_ToDiscountDto_MapsCoreFields()
    {
        var discount = new DiscountCodeBuilder()
            .WithCode("SAVE10")
            .WithValue(DiscountValue.Percentage(10m))
            .WithMaximumDiscountAmount(50_000m)
            .WithUsageLimit(100)
            .WithExpiresAt(DateTime.UtcNow.AddDays(10))
            .Build();

        var dto = _mapper.Map<DiscountDto>(discount);

        dto.Id.ShouldBe(discount.Id.Value);
        dto.Code.ShouldBe(discount.Code);
        dto.DiscountType.ShouldBe("Percentage");
        dto.DiscountValue.ShouldBe(10m);
        dto.MaximumDiscountAmount.ShouldBe(50_000m);
        dto.UsageLimit.ShouldBe(100);
        dto.UsageCount.ShouldBe(0);
        dto.IsActive.ShouldBeTrue();
        dto.CreatedAt.ShouldBe(discount.CreatedAt);
    }

    [Fact]
    public void Map_DiscountCode_ToDiscountCodeDto_MapsWithoutMaximum()
    {
        var discount = new DiscountCodeBuilder().WithCode("FIX").WithValue(DiscountValue.Fixed(20_000m)).Build();

        var dto = _mapper.Map<DiscountCodeDto>(discount);

        dto.Id.ShouldBe(discount.Id.Value);
        dto.DiscountType.ShouldBe("FixedAmount");
        dto.DiscountValue.ShouldBe(20_000m);
    }

    [Fact]
    public void Map_DiscountCode_ToDiscountCodeDetailDto_MapsRestrictionsList()
    {
        var discount = new DiscountCodeBuilder().Build();

        var dto = _mapper.Map<DiscountCodeDetailDto>(discount);

        dto.Id.ShouldBe(discount.Id.Value);
        dto.Restrictions.ShouldNotBeNull();
        dto.Restrictions.ShouldBeEmpty();
    }

    [Fact]
    public void Map_DiscountCode_ToDiscountInfoDto_MapsInfoSubset()
    {
        var discount = new DiscountCodeBuilder().WithCode("INFO").Build();

        var dto = _mapper.Map<DiscountInfoDto>(discount);

        dto.Code.ShouldBe(discount.Code);
        dto.DiscountType.ShouldBe(discount.Value.Type.ToString());
        dto.DiscountValue.ShouldBe(discount.Value.Amount);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new DiscountMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
