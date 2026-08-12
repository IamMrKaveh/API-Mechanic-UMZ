using Application.Inventory.Features.Queries.GetLowStockProducts;

namespace Tests.Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsValidatorTests
{
    private readonly GetLowStockProductsValidator _sut = new();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(1000)]
    public void Validate_WithThresholdInRange_ReturnsIsValidTrue(int threshold)
    {
        var query = new GetLowStockProductsQuery(threshold);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_WithNonPositiveThreshold_ReturnsError(int threshold)
    {
        var query = new GetLowStockProductsQuery(threshold);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetLowStockProductsQuery.Threshold));
    }

    [Fact]
    public void Validate_WithThresholdAboveMax_ReturnsError()
    {
        var query = new GetLowStockProductsQuery(1001);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetLowStockProductsQuery.Threshold));
    }
}
