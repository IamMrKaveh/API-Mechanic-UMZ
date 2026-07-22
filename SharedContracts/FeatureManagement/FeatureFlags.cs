namespace SharedContracts.FeatureManagement;

public static class FeatureFlags
{
    public const string PaymentCallbackSignatureRequired = "Payment.Callback.SignatureRequired";
    public const string IdempotencyDistributedLockEnabled = "Idempotency.DistributedLock.Enabled";
    public const string SagaAutoRefundOnCommitFailure = "Saga.AutoRefundOnCommitFailure";
    public const string StoragePresignedUrlEnabled = "Storage.PresignedUrl.Enabled";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        PaymentCallbackSignatureRequired,
        IdempotencyDistributedLockEnabled,
        SagaAutoRefundOnCommitFailure,
        StoragePresignedUrlEnabled
    };
}
