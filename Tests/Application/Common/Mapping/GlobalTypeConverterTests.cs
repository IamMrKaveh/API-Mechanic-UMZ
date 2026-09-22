using Application.Common.Mapping;
using Application.Common.Mapping.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Mapster;

namespace Tests.Application.Common.Mapping;

public class GlobalTypeConverterTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public GlobalTypeConverterTests()
    {
        _config = new TypeAdapterConfig();
        new GlobalTypeConverter().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Guid_ToString_ReturnsInvariantString()
    {
        var id = Guid.NewGuid();

        _mapper.Map<string>(id).ShouldBe(id.ToString());
    }

    [Fact]
    public void Map_String_ToGuid_ParsesValue()
    {
        var id = Guid.NewGuid();

        _mapper.Map<Guid>(id.ToString()).ShouldBe(id);
    }

    [Fact]
    public void Map_DateTime_ToString_UsesUtcSortableFormat()
    {
        var date = new DateTime(2026, 3, 15, 10, 30, 45, DateTimeKind.Utc);

        _mapper.Map<string>(date).ShouldBe("2026-03-15T10:30:45Z");
    }

    [Fact]
    public void Map_Percentage_ToPercentageDto_MapsValue()
    {
        var percentage = Percentage.Create(12.5m);

        var dto = _mapper.Map<PercentageDto>(percentage);

        dto.Value.ShouldBe(12.5m);
    }

    [Fact]
    public void Map_String_ToSlug_ReturnsNormalizedSlug()
    {
        var slug = _mapper.Map<Slug>("brake-pads");

        slug.ShouldNotBeNull();
        slug.Value.ShouldBe("brake-pads");
    }

    [Fact]
    public void Map_String_ToProductName_CreatesValueObject()
    {
        var name = _mapper.Map<ProductName>("Brake Pad");

        name.Value.ShouldBe("Brake Pad");
    }

    [Fact]
    public void Map_String_ToCategoryName_CreatesValueObject()
    {
        var name = _mapper.Map<CategoryName>("Brakes");

        name.Value.ShouldBe("Brakes");
    }

    [Fact]
    public void Map_String_ToBrandName_CreatesValueObject()
    {
        var name = _mapper.Map<BrandName>("Bosch");

        name.Value.ShouldBe("Bosch");
    }

    [Fact]
    public void Map_String_ToBrandSlug_CreatesSlug()
    {
        var slug = _mapper.Map<BrandSlug>("bosch-tools");

        slug.Value.ShouldBe("bosch-tools");
    }

    [Fact]
    public void Map_String_ToCategorySlug_CreatesSlug()
    {
        var slug = _mapper.Map<CategorySlug>("brake-pads");

        slug.Value.ShouldBe("brake-pads");
    }

    [Fact]
    public void Map_String_ToProductSlug_CreatesSlug()
    {
        var slug = _mapper.Map<ProductSlug>("oil-filter-x");

        slug.Value.ShouldBe("oil-filter-x");
    }

    [Fact]
    public void Map_Guid_ToCategoryId_PreservesValue()
    {
        var id = Guid.NewGuid();

        _mapper.Map<CategoryId>(id).Value.ShouldBe(id);
    }

    [Fact]
    public void Map_Guid_ToBrandId_PreservesValue()
    {
        var id = Guid.NewGuid();

        _mapper.Map<BrandId>(id).Value.ShouldBe(id);
    }

    [Fact]
    public void Map_Decimal_ToMoney_UsesIrtCurrency()
    {
        var money = _mapper.Map<Money>(1500m);

        money.Amount.ShouldBe(1500m);
        money.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Map_Money_ToMoneyDto_MapsAmountAndCurrency()
    {
        var money = Money.Create(2500m, "IRT");

        var dto = _mapper.Map<MoneyDto>(money);

        dto.Amount.ShouldBe(2500m);
        dto.Currency.ShouldBe("IRT");
    }

    [Fact]
    public void Map_Money_ToDecimal_ReturnsAmount()
    {
        var money = Money.Create(999m, "IRT");

        _mapper.Map<decimal>(money).ShouldBe(999m);
    }

    [Fact]
    public void Map_NullableMoney_ToNullableDecimal_MapsNullAndValue()
    {
        Money? nullMoney = null;
        Money? money = Money.Create(123m, "IRT");

        _mapper.Map<decimal?>((object?)nullMoney).ShouldBeNull();
        _mapper.Map<decimal?>(money).ShouldBe(123m);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new GlobalTypeConverter().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
