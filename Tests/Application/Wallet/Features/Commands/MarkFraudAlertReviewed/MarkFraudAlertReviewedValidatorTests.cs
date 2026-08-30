using Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

namespace Tests.Application.Wallet.Features.Commands.MarkFraudAlertReviewed;

public class MarkFraudAlertReviewedValidatorTests
{
    private readonly MarkFraudAlertReviewedValidator _sut = new();

    private static MarkFraudAlertReviewedCommand ValidCommand(
        Guid? alertId = null,
        string? note = "checked") =>
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkFraudAlertReviewedCommand.AlertId));
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
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkFraudAlertReviewedCommand.Note));
    }
}
