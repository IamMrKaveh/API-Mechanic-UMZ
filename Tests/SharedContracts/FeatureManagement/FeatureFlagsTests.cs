using SharedContracts.FeatureManagement;

namespace Tests.SharedContracts.FeatureManagement;

public class FeatureFlagsTests
{
    [Fact]
    public void FlagNames_HaveExpectedValues()
    {
        FeatureFlags.PaymentCallbackSignatureRequired.ShouldBe("Payment.Callback.SignatureRequired");
        FeatureFlags.PaymentCallbackIpWhitelistRequired.ShouldBe("Payment.Callback.IpWhitelistRequired");
        FeatureFlags.IdempotencyDistributedLockEnabled.ShouldBe("Idempotency.DistributedLock.Enabled");
        FeatureFlags.SagaAutoRefundOnCommitFailure.ShouldBe("Saga.AutoRefundOnCommitFailure");
        FeatureFlags.StoragePresignedUrlEnabled.ShouldBe("Storage.PresignedUrl.Enabled");
        FeatureFlags.AdminWalletLedgerV2Enabled.ShouldBe("AdminWallet.LedgerV2Enabled");
    }

    [Fact]
    public void All_ContainsAllSixFlags()
    {
        var all = FeatureFlags.All;

        all.Count.ShouldBe(6);
        all.ShouldContain(FeatureFlags.PaymentCallbackSignatureRequired);
        all.ShouldContain(FeatureFlags.PaymentCallbackIpWhitelistRequired);
        all.ShouldContain(FeatureFlags.IdempotencyDistributedLockEnabled);
        all.ShouldContain(FeatureFlags.SagaAutoRefundOnCommitFailure);
        all.ShouldContain(FeatureFlags.StoragePresignedUrlEnabled);
        all.ShouldContain(FeatureFlags.AdminWalletLedgerV2Enabled);
    }

    [Fact]
    public void All_ContainsDistinctValues()
    {
        FeatureFlags.All.Distinct().Count().ShouldBe(FeatureFlags.All.Count);
    }
}
