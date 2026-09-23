using Application.Payment.Features.Shared;
using Application.Payment.Mapping;
using Mapster;

namespace Tests.Application.Payment.Mapping;

public class PaymentMethodMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public PaymentMethodMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new PaymentMethodMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_PaymentMethod_ToPaymentMethodDto_MapsFeeSplit()
    {
        var method = new PaymentMethodBuilder()
            .WithName("Zarinpal")
            .WithCode("zarinpal")
            .WithFee(1_000m, 2m)
            .WithDescription("Online gateway")
            .WithSortOrder(3)
            .Build();

        var dto = _mapper.Map<PaymentMethodDto>(method);

        dto.Id.ShouldBe(method.Id.Value);
        dto.Name.ShouldBe("Zarinpal");
        dto.Code.ShouldBe("zarinpal");
        dto.Description.ShouldBe("Online gateway");
        dto.FeeAmount.ShouldBe(1_000m);
        dto.FeePercentage.ShouldBe(2m);
        dto.IsActive.ShouldBeTrue();
        dto.SortOrder.ShouldBe(3);
        dto.CreatedAt.ShouldBe(method.CreatedAt);
        dto.UpdatedAt.ShouldBe(method.UpdatedAt);
    }

    [Fact]
    public void Map_PaymentMethod_ToListItemDto_MapsDeletionFlag()
    {
        var method = new PaymentMethodBuilder().Build();

        var dto = _mapper.Map<PaymentMethodListItemDto>(method);

        dto.Id.ShouldBe(method.Id.Value);
        dto.Name.ShouldBe(method.Name.Value);
        dto.Code.ShouldBe(method.Code.Value);
        dto.FeeAmount.ShouldBe(method.Fee.Amount.Amount);
        dto.FeePercentage.ShouldBe(method.Fee.Percentage);
        dto.IsActive.ShouldBeTrue();
        dto.IsDeleted.ShouldBeFalse();
        dto.SortOrder.ShouldBe(method.SortOrder);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new PaymentMethodMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
