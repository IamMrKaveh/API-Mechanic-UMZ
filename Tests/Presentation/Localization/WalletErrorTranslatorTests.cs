using Presentation.Localization;
using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Tests.Presentation.Localization;

public class WalletErrorTranslatorTests
{
    private static DomainException BuildException(string errorCode, string message = "raw", IReadOnlyDictionary<string, object?>? args = null)
        => new(errorCode, message, args, null);

    [Fact]
    public void Translate_WithNull_ReturnsEmptyString()
    {
        WalletErrorTranslator.Translate(null!).ShouldBe(string.Empty);
    }

    [Fact]
    public void Translate_WithUnknownCode_ReturnsOriginalMessage()
    {
        var exception = BuildException("UNKNOWN.CODE", "original message");

        WalletErrorTranslator.Translate(exception).ShouldBe("original message");
    }

    [Fact]
    public void Translate_TopUpUnknownFailure_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.TopUpUnknownFailure, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("خطای نامشخص در پرداخت.");
    }

    [Fact]
    public void Translate_TransferSelfNotAllowed_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.TransferSelfNotAllowed, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("انتقال به کیف پول خود مجاز نیست.");
    }

    [Fact]
    public void Translate_TransferInvalidOtpTtl_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.TransferInvalidOtpTtl, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("مدت اعتبار کد تأیید نامعتبر است.");
    }

    [Fact]
    public void Translate_TransferOtpExpired_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.TransferOtpExpired, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("مهلت وارد کردن کد تأیید به پایان رسیده است.");
    }

    [Fact]
    public void Translate_TransferOnlyCreatorCanCancel_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.TransferOnlyCreatorCanCancel, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("فقط ایجادکننده انتقال می‌تواند آن را لغو کند.");
    }

    [Fact]
    public void Translate_TransferInvalidState_IncludesStatusArg()
    {
        var args = new Dictionary<string, object?> { ["status"] = "Pending" };
        var exception = BuildException(DomainErrorCodes.Wallet.TransferInvalidState, "raw", args);

        WalletErrorTranslator.Translate(exception).ShouldContain("Pending");
    }

    [Fact]
    public void Translate_TransferInvalidState_WithMissingStatus_UsesFallback()
    {
        var exception = BuildException(
            DomainErrorCodes.Wallet.TransferInvalidState,
            "raw",
            new Dictionary<string, object?>());

        WalletErrorTranslator.Translate(exception).ShouldContain("?");
    }

    [Fact]
    public void Translate_WithdrawalUserIdRequired_ReturnsPersianMessage()
    {
        var exception = BuildException(DomainErrorCodes.Wallet.WithdrawalUserIdRequired, "raw");

        WalletErrorTranslator.Translate(exception).ShouldBe("شناسه کاربر الزامی است.");
    }

    [Fact]
    public void Translate_WithdrawalMinimumAmount_IncludesMinimumArg()
    {
        var args = new Dictionary<string, object?> { ["minimum"] = 50000 };
        var exception = BuildException(DomainErrorCodes.Wallet.WithdrawalMinimumAmount, "raw", args);

        var translated = WalletErrorTranslator.Translate(exception);

        translated.ShouldContain("تومان");
    }

    [Fact]
    public void Translate_InsufficientBalance_IncludesWalletId()
    {
        var args = new Dictionary<string, object?>
        {
            ["walletId"] = "wallet-1",
            ["requestedAmount"] = 100000,
            ["requestedCurrency"] = "IRT",
            ["availableAmount"] = 20000,
            ["availableCurrency"] = "IRT"
        };
        var exception = BuildException(DomainErrorCodes.Wallet.InsufficientBalance, "raw", args);

        var translated = WalletErrorTranslator.Translate(exception);

        translated.ShouldContain("wallet-1");
        translated.ShouldContain("موجودی کافی ندارد");
    }

    [Fact]
    public void Translate_InactiveWallet_IncludesWalletId()
    {
        var args = new Dictionary<string, object?> { ["walletId"] = "wallet-1" };
        var exception = BuildException(DomainErrorCodes.Wallet.Inactive, "raw", args);

        WalletErrorTranslator.Translate(exception).ShouldContain("wallet-1");
    }

    [Fact]
    public void Translate_TransferOtpMismatch_IncludesRemainingAttempts()
    {
        var args = new Dictionary<string, object?> { ["remainingAttempts"] = 2 };
        var exception = BuildException(DomainErrorCodes.Wallet.TransferOtpMismatch, "raw", args);

        WalletErrorTranslator.Translate(exception).ShouldContain("2");
    }

    [Fact]
    public void Translate_DebitRequestNotFound_IncludesRequestId()
    {
        var args = new Dictionary<string, object?> { ["requestId"] = "req-1" };
        var exception = BuildException(DomainErrorCodes.Wallet.DebitRequestNotFound, "raw", args);

        WalletErrorTranslator.Translate(exception).ShouldContain("req-1");
    }

    [Fact]
    public void Translate_ReservationNotFound_IncludesReservationId()
    {
        var args = new Dictionary<string, object?> { ["reservationId"] = "res-1" };
        var exception = BuildException(DomainErrorCodes.Wallet.ReservationNotFound, "raw", args);

        WalletErrorTranslator.Translate(exception).ShouldContain("res-1");
    }

    [Fact]
    public void Translate_WithdrawalInvalidStateForAction_IncludesStatusAndAction()
    {
        var args = new Dictionary<string, object?> { ["status"] = "Paid", ["action"] = "لغو" };
        var exception = BuildException(DomainErrorCodes.Wallet.WithdrawalInvalidStateForAction, "raw", args);

        var translated = WalletErrorTranslator.Translate(exception);

        translated.ShouldContain("Paid");
        translated.ShouldContain("لغو");
    }
}
