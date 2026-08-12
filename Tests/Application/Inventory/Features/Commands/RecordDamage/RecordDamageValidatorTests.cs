using Application.Inventory.Features.Commands.RecordDamage;

namespace Tests.Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageValidatorTests
{
    private readonly RecordDamageValidator _sut = new();

    [Fact]
    public void Validate_WithValidValues_ReturnsIsValidTrue()
    {
        var command = new RecordDamageCommand(Guid.NewGuid(), 2, "broken");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_ReturnsError()
    {
        var command = new RecordDamageCommand(Guid.Empty, 1, "broken");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RecordDamageCommand.VariantId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Validate_WithNonPositiveQuantity_ReturnsError(int quantity)
    {
        var command = new RecordDamageCommand(Guid.NewGuid(), quantity, "broken");

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RecordDamageCommand.Quantity));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyReason_ReturnsError(string? reason)
    {
        var command = new RecordDamageCommand(Guid.NewGuid(), 1, reason!);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(RecordDamageCommand.Reason));
    }
}
