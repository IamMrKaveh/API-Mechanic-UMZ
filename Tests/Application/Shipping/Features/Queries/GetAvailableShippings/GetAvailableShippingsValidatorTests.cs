using Application.Shipping.Features.Queries.GetAvailableShippings;

namespace Tests.Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsValidatorTests
{
    private readonly GetAvailableShippingsValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void Validate_WhenOrderAmountIsNonNegative_IsValid(decimal amount)
    {
        var result = _sut.Validate(new GetAvailableShippingsQuery(amount));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenOrderAmountIsNegative_HasErrorForOrderAmount()
    {
        var result = _sut.Validate(new GetAvailableShippingsQuery(-1m));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetAvailableShippingsQuery.OrderAmount));
    }
}
