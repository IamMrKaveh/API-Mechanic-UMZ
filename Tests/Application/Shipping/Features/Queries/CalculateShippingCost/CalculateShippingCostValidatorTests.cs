using Application.Shipping.Features.Queries.CalculateShippingCost;

namespace Tests.Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostValidatorTests
{
    private readonly CalculateShippingCostValidator _sut = new();

    private static CalculateShippingCostQuery Query(
        Guid? shippingId = null,
        decimal orderAmount = 100_000m) =>
        new(shippingId ?? Guid.NewGuid(), orderAmount);

    [Fact]
    public void Validate_WithValidQuery_IsValid()
    {
        var result = _sut.Validate(Query());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyShippingId_IsInvalid()
    {
        var result = _sut.Validate(Query(shippingId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CalculateShippingCostQuery.ShippingId));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    [InlineData(-1_000_000_000)]
    public void Validate_WithNegativeOrderAmount_IsInvalid(decimal orderAmount)
    {
        var result = _sut.Validate(Query(orderAmount: orderAmount));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CalculateShippingCostQuery.OrderAmount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(50_000)]
    [InlineData(1_000_000_000)]
    public void Validate_WithNonNegativeOrderAmount_IsValid(decimal orderAmount)
    {
        var result = _sut.Validate(Query(orderAmount: orderAmount));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyShippingIdAndNegativeOrderAmount_ReportsBothErrors()
    {
        var result = _sut.Validate(new CalculateShippingCostQuery(Guid.Empty, -1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CalculateShippingCostQuery.ShippingId));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CalculateShippingCostQuery.OrderAmount));
    }
}
