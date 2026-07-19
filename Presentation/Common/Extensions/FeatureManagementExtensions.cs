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

        public static IReadOnlyList<string> All { get; } = new[]
        {
            PaymentCallbackSignatureRequired,
            IdempotencyDistributedLockEnabled,
            SagaAutoRefundOnCommitFailure,
            StoragePresignedUrlEnabled
        };
    }

    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFeatureManagement(configuration.GetSection(SectionName));
        return services;
    }

    public static void ValidateFeatureFlagsPresence(this IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        if (!section.Exists())
            throw new InvalidOperationException(
                $"Configuration section '{SectionName}' is missing.");

        var missing = new List<string>();
        foreach (var flag in Flags.All)
        {
            var value = section[flag];
            if (value is null)
                missing.Add(flag);
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing feature flag entries in '{SectionName}': {string.Join(", ", missing)}.");
    }
}
