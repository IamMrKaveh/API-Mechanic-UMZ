using Application.User.Features.Commands.UpdateProfile; using FluentValidation.Results;

namespace Tests.Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileValidatorTests { private readonly UpdateProfileValidator _sut = new();

[Fact]
public void Validate_WithBothNames_Null_ReturnsValid()
{
    var command = new UpdateProfileCommand(null, null);

    ValidationResult result = _sut.Validate(command);

    result.IsValid.ShouldBeTrue();
    result.Errors.ShouldBeEmpty();
}

[Theory]
[InlineData("Ali", "Ahmadi")]
[InlineData("", "")]
[InlineData("A", "B")]
public void Validate_WithNamesWithinMaximumLength_ReturnsValid(string firstName, string lastName)
{
    var command = new UpdateProfileCommand(firstName, lastName);

    var result = _sut.Validate(command);

    result.IsValid.ShouldBeTrue();
}

[Fact]
public void Validate_WhenFirstNameExceedsMaximumLength_ReturnsInvalidForFirstName()
{
    var firstName = new string('a', 51);
    var command = new UpdateProfileCommand(firstName, "Ahmadi");

    var result = _sut.Validate(command);

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProfileCommand.FirstName));
}

[Fact]
public void Validate_WhenLastNameExceedsMaximumLength_ReturnsInvalidForLastName()
{
    var lastName = new string('b', 51);
    var command = new UpdateProfileCommand("Ali", lastName);

    var result = _sut.Validate(command);

    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProfileCommand.LastName));
}

[Fact]
public void Validate_WhenFirstNameIsExactlyMaximumLength_ReturnsValid()
{
    var firstName = new string('a', 50);
    var command = new UpdateProfileCommand(firstName, null);

    var result = _sut.Validate(command);

    result.IsValid.ShouldBeTrue();
}

}