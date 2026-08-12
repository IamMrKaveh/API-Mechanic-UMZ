using Application.Payment.Features.Commands.AtomicRefundPayment;
using FluentValidation.TestHelper;

namespace Tests.Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentValidatorTests
{
    private readonly AtomicRefundPaymentValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var cmd = new AtomicRefundPaymentCommand(Guid.NewGuid(), "customer request");

        var result = _sut.TestValidate(cmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyOrderId_HasError()
    {
        var cmd = new AtomicRefundPaymentCommand(Guid.Empty, "reason");

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyReason_HasError(string? reason)
    {
        var cmd = new AtomicRefundPaymentCommand(Guid.NewGuid(), reason!);

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Validate_ReasonExceedsMaxLength_HasError()
    {
        var cmd = new AtomicRefundPaymentCommand(Guid.NewGuid(), new string('a', 501));

        var result = _sut.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
