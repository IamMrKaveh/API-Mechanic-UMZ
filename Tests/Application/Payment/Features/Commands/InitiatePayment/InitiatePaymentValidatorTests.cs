using Application.Payment.Features.Commands.InitiatePayment;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentValidatorTests
{
    private readonly InitiatePaymentValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var cmd = new InitiatePaymentCommand(Guid.NewGuid(), "zarinpal");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasError()
    {
        var cmd = new InitiatePaymentCommand(Guid.Empty, "zarinpal");

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void Validate_NullGatewayName_HasNoError()
    {
        var cmd = new InitiatePaymentCommand(Guid.NewGuid(), null);

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveValidationErrorFor(x => x.GatewayName);
    }
}
