using Application.Payment.Features.Queries.GetActivePaymentMethods;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Queries.GetActivePaymentMethods;

public class GetActivePaymentMethodsValidatorTests
{
    private readonly GetActivePaymentMethodsValidator _sut = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void Validate_NonNegativeOrderAmount_HasNoError(decimal amount)
    {
        var result = _sut.TestValidate(new GetActivePaymentMethodsQuery(amount));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NegativeOrderAmount_HasError()
    {
        var result = _sut.TestValidate(new GetActivePaymentMethodsQuery(-0.01m));

        result.ShouldHaveValidationErrorFor(x => x.OrderAmount);
    }
}
