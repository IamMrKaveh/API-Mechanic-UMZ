using Application.Order.Features.Commands.MarkOrderAsShipped;

namespace Tests.Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedValidatorTests
{
    private readonly MarkOrderAsShippedValidator _sut = new();

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_HasErrorForOrderId()
    {
        var result = _sut.Validate(new MarkOrderAsShippedCommand(Guid.Empty, null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkOrderAsShippedCommand.OrderId));
    }

    [Fact]
    public void Validate_WhenOrderIdIsEmptyAndRowVersionProvided_HasErrorForOrderId()
    {
        var result = _sut.Validate(new MarkOrderAsShippedCommand(Guid.Empty, "AA=="));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkOrderAsShippedCommand.OrderId));
    }

    [Fact]
    public void Validate_WhenOrderIdProvidedAndRowVersionIsNull_IsValid()
    {
        var result = _sut.Validate(new MarkOrderAsShippedCommand(Guid.NewGuid(), null));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("AA==")]
    [InlineData("!!!not-base64!!!")]
    public void Validate_WhenOrderIdProvided_RowVersionIsNotValidatedByThisValidator(string? rowVersion)
    {
        // Base64 shape of RowVersion is enforced inside the handler, not the validator.
        var result = _sut.Validate(new MarkOrderAsShippedCommand(Guid.NewGuid(), rowVersion));

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(MarkOrderAsShippedCommand.RowVersion));
    }
}
