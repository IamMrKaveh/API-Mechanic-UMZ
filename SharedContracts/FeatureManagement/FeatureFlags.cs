namespace SharedContracts.FeatureManagement;

public static class FeatureFlags
{
    public const string PaymentCallbackSignatureRequired = "Payment.Callback.SignatureRequired";
    public const string IdempotencyDistributedLockEnabled = "Idempotency.DistributedLock.Enabled";
    public const string SagaAutoRefundOnCommitFailure = "Saga.AutoRefundOnCommitFailure";
    public const string StoragePresignedUrlEnabled = "Storage.PresignedUrl.Enabled";
    public const string AdminWalletLedgerV2Enabled = "AdminWallet.LedgerV2Enabled";

    public static IReadOnlyList<string> All { get; } =
    [
        PaymentCallbackSignatureRequired,
        IdempotencyDistributedLockEnabled,
        SagaAutoRefundOnCommitFailure,
        StoragePresignedUrlEnabled,
        AdminWalletLedgerV2Enabled
    ];
}
