using Microsoft.FeatureManagement;

namespace Presentation.Common.Extensions;

public static class FeatureManagementExtensions
{
    public const string SectionName = "FeatureManagement";

    public static class Flags
    {
        public const string PaymentCallbackSignatureRequired = "Payment.Callback.SignatureRequired";
        public const string IdempotencyDistributedLockEnabled = "Idempotency.DistributedLock.Enabled";
        public const string SagaAutoRefundOnCommitFailure = "Saga.AutoRefundOnCommitFailure";
        public const string StoragePresignedUrlEnabled = "Storage.PresignedUrl.Enabled";
    }

    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection(SectionName));
        return services;
    }
}
