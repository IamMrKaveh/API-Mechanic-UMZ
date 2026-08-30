using Application.Order.Features.Commands.ConfirmDelivery;

namespace Tests.Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryValidatorTests
{
    private readonly ConfirmDeliveryValidator _sut = new();

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_HasErrorForOrderId()
    {
        var result = _sut.Validate(new ConfirmDeliveryCommand(Guid.Empty, null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmDeliveryCommand.OrderId));
    }

    [Fact]
    public void Validate_WhenOrderIdIsEmptyAndRowVersionProvided_HasErrorForOrderId()
    {
        var result = _sut.Validate(new ConfirmDeliveryCommand(Guid.Empty, "AA=="));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ConfirmDeliveryCommand.OrderId));
    }

    [Fact]
    public void Validate_WhenOrderIdProvidedAndRowVersionIsNull_IsValid()
    {
        var result = _sut.Validate(new ConfirmDeliveryCommand(Guid.NewGuid(), null));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("AA==")]
    [InlineData("some-non-base64-but-not-validated-here")]
    public void Validate_WhenOrderIdProvided_RowVersionIsNotValidatedByThisValidator(string? rowVersion)
    {
        // Base64 shape of RowVersion is enforced inside the handler,
        // not by the validator — the validator should accept any string.
        var result = _sut.Validate(new ConfirmDeliveryCommand(Guid.NewGuid(), rowVersion));

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(ConfirmDeliveryCommand.RowVersion));
    }
}
