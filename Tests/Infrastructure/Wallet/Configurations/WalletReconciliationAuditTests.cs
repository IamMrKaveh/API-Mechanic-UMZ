using Infrastructure.Wallet.Configurations;

namespace Tests.Infrastructure.Wallet.Configurations;

public class WalletReconciliationAuditTests
{
    [Fact]
    public void Defaults_GenerateIdAndDetectionTimestamp()
    {
        var before = DateTime.UtcNow;

        var sut = new WalletReconciliationAudit();

        sut.Id.ShouldNotBe(Guid.Empty);
        sut.DetectedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.DetectedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        sut.WalletId.ShouldBe(Guid.Empty);
        sut.UserId.ShouldBe(Guid.Empty);
        sut.SnapshotBalance.ShouldBe(0m);
        sut.LedgerBalance.ShouldBe(0m);
        sut.Delta.ShouldBe(0m);
        sut.Notes.ShouldBeNull();
    }

    [Fact]
    public void Instances_ReceiveDistinctIds()
    {
        new WalletReconciliationAudit().Id.ShouldNotBe(new WalletReconciliationAudit().Id);
    }

    [Fact]
    public void Properties_RoundtripAssignedValues()
    {
        var id = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var detectedAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var sut = new WalletReconciliationAudit
        {
            Id = id,
            WalletId = walletId,
            UserId = userId,
            SnapshotBalance = 100_000m,
            LedgerBalance = 99_500m,
            Delta = 500m,
            DetectedAt = detectedAt,
            Notes = "Mismatch under investigation"
        };

        sut.Id.ShouldBe(id);
        sut.WalletId.ShouldBe(walletId);
        sut.UserId.ShouldBe(userId);
        sut.SnapshotBalance.ShouldBe(100_000m);
        sut.LedgerBalance.ShouldBe(99_500m);
        sut.Delta.ShouldBe(500m);
        sut.DetectedAt.ShouldBe(detectedAt);
        sut.Notes.ShouldBe("Mismatch under investigation");
    }

    public static TheoryData<decimal, decimal, decimal> DeltaCases() => new()
    {
        { 100m, 100m, 0m },
        { 100m, 90m, 10m },
        { 90m, 100m, -10m },
        { 0m, 0m, 0m },
    };

    [Theory]
    [MemberData(nameof(DeltaCases))]
    public void Delta_CapturesSignedDifferences(decimal snapshot, decimal ledger, decimal expectedDelta)
    {
        var sut = new WalletReconciliationAudit
        {
            SnapshotBalance = snapshot,
            LedgerBalance = ledger,
            Delta = snapshot - ledger
        };

        sut.Delta.ShouldBe(expectedDelta);
    }

    [Fact]
    public void Notes_WhenAbsent_StaysNull()
    {
        var sut = new WalletReconciliationAudit { Notes = null };

        sut.Notes.ShouldBeNull();
    }

    [Theory]
    [InlineData(nameof(WalletReconciliationAudit.Id))]
    [InlineData(nameof(WalletReconciliationAudit.WalletId))]
    [InlineData(nameof(WalletReconciliationAudit.Delta))]
    [InlineData(nameof(WalletReconciliationAudit.DetectedAt))]
    [InlineData(nameof(WalletReconciliationAudit.Notes))]
    public void Properties_UseInitOnlySetters(string propertyName)
    {
        var setter = typeof(WalletReconciliationAudit).GetProperty(propertyName)!.SetMethod;

        setter.ShouldNotBeNull();
        setter!.ReturnParameter.GetRequiredCustomModifiers()
            .ShouldContain(typeof(System.Runtime.CompilerServices.IsExternalInit));
    }
}
