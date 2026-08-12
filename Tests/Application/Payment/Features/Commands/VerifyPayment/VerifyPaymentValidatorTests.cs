using Application.Payment.Features.Commands.VerifyPayment;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentValidatorTests
{
    private readonly VerifyPaymentValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var cmd = new VerifyPaymentCommand("A00000000000000000000000000000000001", "OK");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyAuthority_HasError(string? authority)
    {
        var cmd = new VerifyPaymentCommand(authority!, "OK");

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Authority);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyStatus_HasError(string? status)
    {
        var cmd = new VerifyPaymentCommand("A00000000000000000000000000000000001", status!);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
