using Application.Order.Features.Commands.CreateOrderStatus;

namespace Tests.Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusValidatorTests { private readonly CreateOrderStatusValidator _sut = new();

private static CreateOrderStatusCommand ValidCommand(string? color = null) =>
    new("paid", "Paid", null, color, 0, false, false);

[Fact]
public void Validate_WhenAllFieldsValid_IsValid()
{
    _sut.Validate(ValidCommand()).IsValid.ShouldBeTrue();
}

[Theory]
[InlineData("")]
[InlineData("   ")]
public void Validate_WhenNameIsBlank_HasErrorForName(string name)
{
    var cmd = ValidCommand() with { Name = name };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.Name));
}

[Fact]
public void Validate_WhenNameExceeds50Chars_HasErrorForName()
{
    var cmd = ValidCommand() with { Name = new string('a', 51) };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.Name));
}

[Fact]
public void Validate_WhenDisplayNameExceeds100Chars_HasErrorForDisplayName()
{
    var cmd = ValidCommand() with { DisplayName = new string('a', 101) };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.DisplayName));
}

[Theory]
[InlineData("not-a-color")]
[InlineData("#GGG")]
[InlineData("123456")]
public void Validate_WhenColorIsNotValidHex_HasErrorForColor(string color)
{
    var cmd = ValidCommand(color);

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.Color));
}

[Theory]
[InlineData("#FFF")]
[InlineData("#FFAA00")]
[InlineData("#00ff00")]
public void Validate_WhenColorIsValidHex_HasNoErrorForColor(string color)
{
    var cmd = ValidCommand(color);

    var result = _sut.Validate(cmd);
    result.Errors.ShouldNotContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.Color));
}

[Fact]
public void Validate_WhenSortOrderIsNegative_HasErrorForSortOrder()
{
    var cmd = ValidCommand() with { SortOrder = -1 };

    _sut.Validate(cmd).Errors.ShouldContain(e => e.PropertyName == nameof(CreateOrderStatusCommand.SortOrder));
}
}