using Application.Wallet.Features.Commands.MarkWithdrawalPaid;

namespace Tests.Application.Wallet.Features.Commands.MarkWithdrawalPaid;

public class MarkWithdrawalPaidValidatorTests
{
    private readonly MarkWithdrawalPaidValidator _sut = new();

    private static MarkWithdrawalPaidCommand ValidCommand(
        Guid? withdrawalId = null,
        string bankReferenceNumber = "REF-1234567890") =>
        new(withdrawalId ?? Guid.NewGuid(), bankReferenceNumber);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyWithdrawalId_IsInvalid()
    {
        var result = _sut.Validate(ValidCommand(withdrawalId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkWithdrawalPaidCommand.WithdrawalId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingBankReferenceNumber_IsInvalid(string reference)
    {
        var result = _sut.Validate(ValidCommand(bankReferenceNumber: reference));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkWithdrawalPaidCommand.BankReferenceNumber));
    }

    [Fact]
    public void Validate_WithBankReferenceNumberAtMaximumLength_IsValid()
    {
        var reference = new string('r', 64);
        var result = _sut.Validate(ValidCommand(bankReferenceNumber: reference));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithBankReferenceNumberLongerThanMaximum_IsInvalid()
    {
        var reference = new string('r', 65);
        var result = _sut.Validate(ValidCommand(bankReferenceNumber: reference));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MarkWithdrawalPaidCommand.BankReferenceNumber));
    }
}
