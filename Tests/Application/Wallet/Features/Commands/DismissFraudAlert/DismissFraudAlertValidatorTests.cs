using Application.Wallet.Features.Commands.DismissFraudAlert;

namespace Tests.Application.Wallet.Features.Commands.DismissFraudAlert;

public class DismissFraudAlertValidatorTests
{
    private readonly DismissFraudAlertValidator _sut = new();

    private static DismissFraudAlertCommand ValidCommand(
        Guid? alertId = null,
        string? note = "false positive") =>
        new(alertId ?? Guid.NewGuid(), note);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullNote_IsValid()
    {
        var result = _sut.Validate(ValidCommand(note: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyAlertId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(alertId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DismissFraudAlertCommand.AlertId));
    }

    [Fact]
    public void Validate_WithNoteAtMaximumLength_IsValid()
    {
        var note = new string('x', 500);
        var result = _sut.Validate(ValidCommand(note: note));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNoteLongerThanMaximum_IsInvalid()
    {
        var note = new string('x', 501);
        var result = _sut.Validate(ValidCommand(note: note));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DismissFraudAlertCommand.Note));
    }
}
