using Application.Payment.Features.Commands.CreatePaymentMethod;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Commands.CreatePaymentMethod;

public class CreatePaymentMethodValidatorTests
{
    private readonly CreatePaymentMethodValidator _sut = new();

    private static CreatePaymentMethodCommand ValidCommand(
        string? name = "Zarinpal",
        string? code = "zarinpal",
        string? description = "درگاه",
        string? iconUrl = null,
        decimal feeAmount = 0m,
        decimal feePercentage = 0m,
        int sortOrder = 0) =>
        new(name!, code!, description, iconUrl, feeAmount, feePercentage, sortOrder);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _sut.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_HasError(string? name)
    {
        var result = _sut.TestValidate(ValidCommand(name: name));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(name: new string('a', 101)));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyCode_HasError(string? code)
    {
        var result = _sut.TestValidate(ValidCommand(code: code));

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_CodeExceedsMaxLength_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(code: new string('a', 51)));

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_NegativeFeeAmount_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(feeAmount: -0.01m));

        result.ShouldHaveValidationErrorFor(x => x.FeeAmount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_FeePercentageOutOfRange_HasError(decimal percentage)
    {
        var result = _sut.TestValidate(ValidCommand(feePercentage: percentage));

        result.ShouldHaveValidationErrorFor(x => x.FeePercentage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_FeePercentageInRange_HasNoError(decimal percentage)
    {
        var result = _sut.TestValidate(ValidCommand(feePercentage: percentage));

        result.ShouldNotHaveValidationErrorFor(x => x.FeePercentage);
    }

    [Fact]
    public void Validate_NegativeSortOrder_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(sortOrder: -1));

        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(description: new string('a', 501)));

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_IconUrlExceedsMaxLength_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(iconUrl: new string('a', 501)));

        result.ShouldHaveValidationErrorFor(x => x.IconUrl);
    }
}
