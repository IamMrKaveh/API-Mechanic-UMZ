using Application.Payment.Features.Commands.UpdatePaymentMethod;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Commands.UpdatePaymentMethod;

public class UpdatePaymentMethodValidatorTests
{
    private readonly UpdatePaymentMethodValidator _sut = new();

    private static UpdatePaymentMethodCommand ValidCommand(
        Guid? id = null,
        string? name = "Zarinpal",
        string? description = "درگاه",
        string? iconUrl = null,
        decimal feeAmount = 0m,
        decimal feePercentage = 0m,
        int sortOrder = 0) =>
        new(id ?? Guid.NewGuid(), name!, description, iconUrl, feeAmount, feePercentage, sortOrder);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _sut.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(id: Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Id);
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

    [Fact]
    public void Validate_NegativeFeeAmount_HasError()
    {
        var result = _sut.TestValidate(ValidCommand(feeAmount: -1m));

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
