using Application.User.Features.Commands.ChangePassword;

namespace Tests.Application.User.Features.Commands.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _sut = new();

    private static ChangePasswordCommand ValidCommand(
        string currentPassword = "OldPass123!",
        string newPassword = "NewPass123!") =>
        new(currentPassword, newPassword);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCurrentPassword_FailsOnCurrentPassword()
    {
        var result = _sut.Validate(ValidCommand(currentPassword: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Fact]
    public void Validate_WithNullCurrentPassword_FailsOnCurrentPassword()
    {
        var result = _sut.Validate(ValidCommand(currentPassword: null!));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Fact]
    public void Validate_WithEmptyNewPassword_FailsOnNewPassword()
    {
        var result = _sut.Validate(ValidCommand(newPassword: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithNullNewPassword_FailsOnNewPassword()
    {
        var result = _sut.Validate(ValidCommand(newPassword: null!));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("1234567")]
    [InlineData("Short1!")]
    public void Validate_WithNewPasswordShorterThanMinimumLength_FailsOnNewPassword(string newPassword)
    {
        var result = _sut.Validate(ValidCommand(newPassword: newPassword));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordAtMinimumLength_IsValid()
    {
        var eightChars = new string('a', 8);

        var result = _sut.Validate(ValidCommand(newPassword: eightChars));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNewPasswordAtMaximumLength_IsValid()
    {
        var hundredChars = new string('a', 100);

        var result = _sut.Validate(ValidCommand(newPassword: hundredChars));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNewPasswordLongerThanMaximumLength_FailsOnNewPassword()
    {
        var overLimit = new string('a', 101);

        var result = _sut.Validate(ValidCommand(newPassword: overLimit));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_WithBothPasswordsEmpty_FailsOnBothProperties()
    {
        var result = _sut.Validate(new ChangePasswordCommand(string.Empty, string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }
}
