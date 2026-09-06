using System.Reflection;
using SharedKernel.Localization;

namespace Tests.SharedKernel.Localization;

public class DomainErrorCodesTests
{
    private static IReadOnlyList<(string Name, string Value)> AllConstants() =>
        typeof(DomainErrorCodes.Wallet)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (f.Name, (string)f.GetValue(null)!))
            .ToList();

    [Fact]
    public void WalletErrorCodes_AreAllNonEmpty()
    {
        var constants = AllConstants();

        constants.Count.ShouldBeGreaterThan(0);
        foreach (var (name, value) in constants)
            value.ShouldNotBeNullOrWhiteSpace($"code {name}");
    }

    [Fact]
    public void WalletErrorCodes_AreAllUnique()
    {
        var values = AllConstants().Select(c => c.Value).ToList();

        values.Distinct().Count().ShouldBe(values.Count);
    }

    [Theory]
    [InlineData(nameof(DomainErrorCodes.Wallet.TopUpUnknownFailure), "WALLET.TOPUP.UNKNOWN_FAILURE")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TopUpInvalidState), "WALLET.TOPUP.INVALID_STATE")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferSelfNotAllowed), "WALLET.TRANSFER.SELF_NOT_ALLOWED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferMinimumAmount), "WALLET.TRANSFER.MINIMUM_AMOUNT")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferInvalidOtpTtl), "WALLET.TRANSFER.INVALID_OTP_TTL")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferOtpExpired), "WALLET.TRANSFER.OTP_EXPIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferOtpAttemptsExceeded), "WALLET.TRANSFER.OTP_ATTEMPTS_EXCEEDED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferOnlyCreatorCanCancel), "WALLET.TRANSFER.ONLY_CREATOR_CAN_CANCEL")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferInvalidStateForCancel), "WALLET.TRANSFER.INVALID_STATE_FOR_CANCEL")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferInvalidState), "WALLET.TRANSFER.INVALID_STATE")]
    [InlineData(nameof(DomainErrorCodes.Wallet.FraudAlertInvalidStateForReview), "WALLET.FRAUD.INVALID_STATE_FOR_REVIEW")]
    [InlineData(nameof(DomainErrorCodes.Wallet.FraudAlertInvalidStateForDismiss), "WALLET.FRAUD.INVALID_STATE_FOR_DISMISS")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalUserIdRequired), "WALLET.WITHDRAWAL.USER_ID_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalAmountRequired), "WALLET.WITHDRAWAL.AMOUNT_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalIbanRequired), "WALLET.WITHDRAWAL.IBAN_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalAccountHolderRequired), "WALLET.WITHDRAWAL.ACCOUNT_HOLDER_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalReservationIdRequired), "WALLET.WITHDRAWAL.RESERVATION_ID_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalMinimumAmount), "WALLET.WITHDRAWAL.MINIMUM_AMOUNT")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalRejectionReasonRequired), "WALLET.WITHDRAWAL.REJECTION_REASON_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalInvalidStateForPay), "WALLET.WITHDRAWAL.INVALID_STATE_FOR_PAY")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalBankReferenceRequired), "WALLET.WITHDRAWAL.BANK_REFERENCE_REQUIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalOnlyOwnerCanCancel), "WALLET.WITHDRAWAL.ONLY_OWNER_CAN_CANCEL")]
    [InlineData(nameof(DomainErrorCodes.Wallet.WithdrawalInvalidStateForAction), "WALLET.WITHDRAWAL.INVALID_STATE_FOR_ACTION")]
    [InlineData(nameof(DomainErrorCodes.Wallet.InsufficientBalance), "INSUFFICIENT_WALLET_BALANCE")]
    [InlineData(nameof(DomainErrorCodes.Wallet.Inactive), "WALLET_INACTIVE")]
    [InlineData(nameof(DomainErrorCodes.Wallet.InvalidAmount), "INVALID_WALLET_AMOUNT")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferLimitExceeded), "WALLET.TRANSFER_LIMIT_EXCEEDED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferOtpMismatch), "WALLET.TRANSFER_OTP_MISMATCH")]
    [InlineData(nameof(DomainErrorCodes.Wallet.TransferOtpAttemptsUsed), "WALLET.TRANSFER_OTP_ATTEMPTS_USED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.InvalidTopUpAmount), "WALLET.INVALID_TOPUP_AMOUNT")]
    [InlineData(nameof(DomainErrorCodes.Wallet.DebitRequestInvalidStatus), "WALLET.DEBIT_REQUEST.INVALID_STATUS")]
    [InlineData(nameof(DomainErrorCodes.Wallet.DebitApprovalUnauthorized), "WALLET.DEBIT_REQUEST.UNAUTHORIZED_APPROVAL")]
    [InlineData(nameof(DomainErrorCodes.Wallet.DebitRequestExpired), "WALLET.DEBIT_REQUEST.EXPIRED")]
    [InlineData(nameof(DomainErrorCodes.Wallet.DebitRequestNotFound), "WALLET.DEBIT_REQUEST.NOT_FOUND")]
    [InlineData(nameof(DomainErrorCodes.Wallet.ReservationNotFound), "WALLET.RESERVATION.NOT_FOUND")]
    public void WalletErrorCode_HasExpectedValue(string name, string expected)
    {
        var field = typeof(DomainErrorCodes.Wallet).GetField(name);

        field.ShouldNotBeNull();
        ((string)field!.GetValue(null)!).ShouldBe(expected);
    }

    [Fact]
    public void WalletErrorCodes_TotalCount_MatchesSource()
    {
        AllConstants().Count.ShouldBe(35);
    }
}
