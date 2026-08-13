using Application.Shipping.Features.Commands.CreateShipping;

namespace Tests.Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingValidatorTests
{
    private readonly CreateShippingValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new CreateShippingCommand("Standard", 50_000m, "desc", "1-3 days", 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WhenNameIsEmpty_HasErrorForName(string? name)
    {
        var command = new CreateShippingCommand(name!, 50_000m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateShippingCommand.Name));
    }

    [Fact]
    public void Validate_WhenNameExceeds100Characters_HasErrorForName()
    {
        var command = new CreateShippingCommand(new string('x', 101), 50_000m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateShippingCommand.Name));
    }

    [Fact]
    public void Validate_WhenBaseCostIsNegative_HasErrorForBaseCost()
    {
        var command = new CreateShippingCommand("Standard", -1m, null, null, 1, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateShippingCommand.BaseCost));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenMinDeliveryDaysIsNotPositive_HasErrorForMinDeliveryDays(int minDays)
    {
        var command = new CreateShippingCommand("Standard", 50_000m, null, null, minDays, 5);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateShippingCommand.MinDeliveryDays));
    }

    [Fact]
    public void Validate_WhenMaxDeliveryDaysIsLessThanMinDeliveryDays_HasErrorForMaxDeliveryDays()
    {
        var command = new CreateShippingCommand("Standard", 50_000m, null, null, 5, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateShippingCommand.MaxDeliveryDays));
    }

    [Fact]
    public void Validate_WhenMaxDeliveryDaysEqualsMinDeliveryDays_IsValid()
    {
        var command = new CreateShippingCommand("Standard", 0m, null, null, 3, 3);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
