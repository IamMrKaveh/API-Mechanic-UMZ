using Application.Shipping.Features.Commands.UpdateShipping;

namespace Tests.Application.Shipping.Features.Commands.UpdateShipping;

public class UpdateShippingValidatorTests
{
    private readonly UpdateShippingValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), "Standard", 50_000m, "desc", "1-3 days", 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_HasErrorForId()
    {
        var command = new UpdateShippingCommand(Guid.Empty, "Standard", 50_000m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenNameIsEmpty_HasErrorForName(string? name)
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), name!, 50_000m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.Name));
    }

    [Fact]
    public void Validate_WhenNameExceeds100Characters_HasErrorForName()
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), new string('x', 101), 50_000m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.Name));
    }

    [Fact]
    public void Validate_WhenBaseCostIsNegative_HasErrorForBaseCost()
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), "Standard", -1m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.BaseCost));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMinDeliveryDaysIsNotPositive_HasErrorForMinDeliveryDays(int minDays)
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), "Standard", 50_000m, null, null, minDays, 5);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.MinDeliveryDays));
    }

    [Fact]
    public void Validate_WhenMaxDeliveryDaysIsLessThanMinDeliveryDays_HasErrorForMaxDeliveryDays()
    {
        var command = new UpdateShippingCommand(Guid.NewGuid(), "Standard", 50_000m, null, null, 5, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateShippingCommand.MaxDeliveryDays));
    }
}
