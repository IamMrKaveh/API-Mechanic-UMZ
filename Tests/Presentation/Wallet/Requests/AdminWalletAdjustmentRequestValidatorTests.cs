using Domain.Wallet.Enums;
using FluentValidation.TestHelper;
using Presentation.Wallet.Requests;

namespace Tests.Presentation.Wallet.Requests;

public class AdminWalletAdjustmentRequestValidatorTests
{
    private readonly AdminWalletAdjustmentRequestValidator _sut = new();

    [Fact]
    public void ValidRequest_ShouldNotHaveErrors()
    {
        var request = new AdminWalletAdjustmentRequest(50000, "year-end bonus", "note", AdminWalletAdjustmentType.Compensation, "ref-1");

        var result = _sut.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroAmount_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(0, "reason text");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("مبلغ باید بزرگ‌تر از صفر باشد.");
    }

    [Fact]
    public void NegativeAmount_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(-100, "reason text");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void AmountAboveMaximum_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(1_000_000_001m, "reason text");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void EmptyReason_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(1000, string.Empty);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("دلیل الزامی است.");
    }

    [Fact]
    public void ShortReason_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(1000, "ab");

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void LongDescription_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(1000, "valid reason", new string('x', 1001));

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void InvalidTransactionType_ShouldHaveError()
    {
        var request = new AdminWalletAdjustmentRequest(1000, "valid reason", null, (AdminWalletAdjustmentType)999);

        var result = _sut.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionType)
            .WithErrorMessage("نوع تراکنش نامعتبر است.");
    }
}
