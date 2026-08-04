namespace SharedKernel.Localization;

public static class DomainErrorCodes
{
    public static class Wallet
    {
        public const string TopUpUnknownFailure = "WALLET.TOPUP.UNKNOWN_FAILURE";
        public const string TopUpInvalidState = "WALLET.TOPUP.INVALID_STATE";

        public const string TransferSelfNotAllowed = "WALLET.TRANSFER.SELF_NOT_ALLOWED";
        public const string TransferMinimumAmount = "WALLET.TRANSFER.MINIMUM_AMOUNT";
        public const string TransferInvalidOtpTtl = "WALLET.TRANSFER.INVALID_OTP_TTL";
        public const string TransferOtpExpired = "WALLET.TRANSFER.OTP_EXPIRED";
        public const string TransferOtpAttemptsExceeded = "WALLET.TRANSFER.OTP_ATTEMPTS_EXCEEDED";
        public const string TransferOnlyCreatorCanCancel = "WALLET.TRANSFER.ONLY_CREATOR_CAN_CANCEL";
        public const string TransferInvalidStateForCancel = "WALLET.TRANSFER.INVALID_STATE_FOR_CANCEL";
        public const string TransferInvalidState = "WALLET.TRANSFER.INVALID_STATE";

        public const string FraudAlertInvalidStateForReview = "WALLET.FRAUD.INVALID_STATE_FOR_REVIEW";
        public const string FraudAlertInvalidStateForDismiss = "WALLET.FRAUD.INVALID_STATE_FOR_DISMISS";

        public const string WithdrawalUserIdRequired = "WALLET.WITHDRAWAL.USER_ID_REQUIRED";
        public const string WithdrawalAmountRequired = "WALLET.WITHDRAWAL.AMOUNT_REQUIRED";
        public const string WithdrawalIbanRequired = "WALLET.WITHDRAWAL.IBAN_REQUIRED";
        public const string WithdrawalAccountHolderRequired = "WALLET.WITHDRAWAL.ACCOUNT_HOLDER_REQUIRED";
        public const string WithdrawalReservationIdRequired = "WALLET.WITHDRAWAL.RESERVATION_ID_REQUIRED";
        public const string WithdrawalMinimumAmount = "WALLET.WITHDRAWAL.MINIMUM_AMOUNT";
        public const string WithdrawalRejectionReasonRequired = "WALLET.WITHDRAWAL.REJECTION_REASON_REQUIRED";
        public const string WithdrawalInvalidStateForPay = "WALLET.WITHDRAWAL.INVALID_STATE_FOR_PAY";
        public const string WithdrawalBankReferenceRequired = "WALLET.WITHDRAWAL.BANK_REFERENCE_REQUIRED";
        public const string WithdrawalOnlyOwnerCanCancel = "WALLET.WITHDRAWAL.ONLY_OWNER_CAN_CANCEL";
        public const string WithdrawalInvalidStateForAction = "WALLET.WITHDRAWAL.INVALID_STATE_FOR_ACTION";

        public const string InsufficientBalance = "WALLET.INSUFFICIENT_BALANCE";
        public const string Inactive = "WALLET.INACTIVE";
        public const string InvalidAmount = "WALLET.INVALID_AMOUNT";
        public const string TransferLimitExceeded = "WALLET.TRANSFER_LIMIT_EXCEEDED";
        public const string TransferOtpMismatch = "WALLET.TRANSFER_OTP_MISMATCH";
        public const string TransferOtpAttemptsUsed = "WALLET.TRANSFER_OTP_ATTEMPTS_USED";
        public const string InvalidTopUpAmount = "WALLET.INVALID_TOPUP_AMOUNT";
        public const string DebitRequestInvalidStatus = "WALLET.DEBIT_REQUEST.INVALID_STATUS";
        public const string DebitApprovalUnauthorized = "WALLET.DEBIT_REQUEST.UNAUTHORIZED_APPROVAL";
        public const string DebitRequestExpired = "WALLET.DEBIT_REQUEST.EXPIRED";
        public const string DebitRequestNotFound = "WALLET.DEBIT_REQUEST.NOT_FOUND";
        public const string ReservationNotFound = "WALLET.RESERVATION.NOT_FOUND";
    }
}
