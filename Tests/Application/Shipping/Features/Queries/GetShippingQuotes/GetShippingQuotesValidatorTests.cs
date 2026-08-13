using Application.Shipping.Features.Queries.GetShippingQuotes;
using Application.Shipping.Features.Shared;

namespace Tests.Application.Shipping.Features.Queries.GetShippingQuotes;

public class GetShippingQuotesValidatorTests
{
    private readonly GetShippingQuotesValidator _sut = new();

    [Fact]
    public void Validate_WithValidQuery_IsValid()
    {
        var query = new GetShippingQuotesQuery(
            100_000m,
            new List<ShippingQuoteItemDto>
            {
            new() { VariantId = Guid.NewGuid(), Quantity = 2 }
            });

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyItems_IsValid()
    {
        var query = new GetShippingQuotesQuery(100_000m, new List<ShippingQuoteItemDto>());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenOrderAmountIsNegative_HasErrorForOrderAmount()
    {
        var query = new GetShippingQuotesQuery(-1m, new List<ShippingQuoteItemDto>());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetShippingQuotesQuery.OrderAmount));
    }

    [Fact]
    public void Validate_WhenItemsIsNull_HasErrorForItems()
    {
        var query = new GetShippingQuotesQuery(100_000m, null!);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetShippingQuotesQuery.Items));
    }

    [Fact]
    public void Validate_WhenAnyItemVariantIdIsEmpty_HasErrorForVariantId()
    {
        var query = new GetShippingQuotesQuery(
            100_000m,
            new List<ShippingQuoteItemDto>
            {
            new() { VariantId = Guid.Empty, Quantity = 1 }
            });

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains(nameof(ShippingQuoteItemDto.VariantId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAnyItemQuantityIsNotPositive_HasErrorForQuantity(int quantity)
    {
        var query = new GetShippingQuotesQuery(
            100_000m,
            new List<ShippingQuoteItemDto>
            {
            new() { VariantId = Guid.NewGuid(), Quantity = quantity }
            });

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains(nameof(ShippingQuoteItemDto.Quantity)));
    }
}
