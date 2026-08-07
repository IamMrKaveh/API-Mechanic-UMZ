using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Domain.Audit.Entities;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Audit.Entities;

public class AuditLogTests
{
    [Fact]
    public void Create_WithValidInput_ProducesLogWithCurrentHashVersion()
    {
        var sut = new AuditLogBuilder()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("10.0.0.1")
            .Build();

        sut.ShouldNotBeNull();
        sut.HashVersion.ShouldBe(AuditLog.CurrentHashVersion);
        sut.IntegrityHash.ShouldNotBeNullOrWhiteSpace();
        sut.VerifyIntegrity().ShouldBeTrue();
    }

    [Fact]
    public void Create_TruncatesCreatedAtToMicroseconds()
    {
        var sut = new AuditLogBuilder().Build();

        (sut.CreatedAt.Ticks % 10L).ShouldBe(0);
        sut.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void VerifyIntegrity_AfterEntityTypeTamper_ReturnsFalse()
    {
        var sut = new AuditLogBuilder()
            .WithEntityType("Order")
            .WithEntityId(Guid.NewGuid().ToString())
            .Build();

        sut.VerifyIntegrity().ShouldBeTrue();

        SetPrivateProperty(sut, nameof(AuditLog.EntityType), "Payment");

        sut.VerifyIntegrity().ShouldBeFalse();
    }

    [Fact]
    public void VerifyIntegrity_AfterEntityIdTamper_ReturnsFalse()
    {
        var sut = new AuditLogBuilder()
            .WithEntityType("Order")
            .WithEntityId("11111111-1111-1111-1111-111111111111")
            .Build();

        SetPrivateProperty(sut, nameof(AuditLog.EntityId), "22222222-2222-2222-2222-222222222222");

        sut.VerifyIntegrity().ShouldBeFalse();
    }

    [Fact]
    public void VerifyIntegrity_AfterUserAgentTamper_ReturnsFalse()
    {
        var sut = new AuditLogBuilder()
            .WithUserAgent("Mozilla/5.0")
            .Build();

        SetPrivateProperty(sut, nameof(AuditLog.UserAgent), "curl/7.85.0");

        sut.VerifyIntegrity().ShouldBeFalse();
    }

    [Fact]
    public void VerifyIntegrity_ForLegacyHashVersion1_UsesLegacyAlgorithm()
    {
        var sut = new AuditLogBuilder()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("10.0.0.1")
            .WithDetails("legacy-details")
            .WithEntityType("SHOULD_BE_IGNORED_IN_V1")
            .WithEntityId("SHOULD_BE_IGNORED_IN_V1")
            .WithUserAgent("SHOULD_BE_IGNORED_IN_V1")
            .Build();

        SetPrivateProperty(sut, nameof(AuditLog.HashVersion), 1);
        var legacyHash = ComputeLegacyHashExternally(sut);
        SetPrivateProperty(sut, nameof(AuditLog.IntegrityHash), legacyHash);

        sut.VerifyIntegrity().ShouldBeTrue();

        SetPrivateProperty(sut, nameof(AuditLog.EntityType), "TAMPERED");
        sut.VerifyIntegrity().ShouldBeTrue();

        SetPrivateProperty(sut, nameof(AuditLog.Details), "TAMPERED");
        sut.VerifyIntegrity().ShouldBeFalse();
    }

    [Fact]
    public void UpgradeHashVersion_FromV1ToV2_RecomputesHashAndPreservesValidity()
    {
        var sut = new AuditLogBuilder()
            .WithEntityType("Order")
            .WithEntityId(Guid.NewGuid().ToString())
            .Build();

        SetPrivateProperty(sut, nameof(AuditLog.HashVersion), 1);
        var legacyHash = ComputeLegacyHashExternally(sut);
        SetPrivateProperty(sut, nameof(AuditLog.IntegrityHash), legacyHash);

        sut.VerifyIntegrity().ShouldBeTrue();

        sut.UpgradeHashVersion();

        sut.HashVersion.ShouldBe(AuditLog.CurrentHashVersion);
        sut.VerifyIntegrity().ShouldBeTrue();
    }

    [Fact]
    public void UpgradeHashVersion_WhenAlreadyLatest_IsNoop()
    {
        var sut = new AuditLogBuilder().Build();
        var originalHash = sut.IntegrityHash;

        sut.UpgradeHashVersion();

        sut.HashVersion.ShouldBe(AuditLog.CurrentHashVersion);
        sut.IntegrityHash.ShouldBe(originalHash);
    }

    [Fact]
    public void MarkAsArchived_SetsFlagsAndIsIdempotent()
    {
        var sut = new AuditLogBuilder().Build();

        sut.MarkAsArchived();
        var firstArchivedAt = sut.ArchivedAt;

        sut.IsArchived.ShouldBeTrue();
        firstArchivedAt.ShouldNotBeNull();

        sut.MarkAsArchived();

        sut.ArchivedAt.ShouldBe(firstArchivedAt);
    }

    private static void SetPrivateProperty(AuditLog log, string propertyName, object? value)
    {
        var prop = typeof(AuditLog).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        prop.ShouldNotBeNull();
        prop!.SetValue(log, value);
    }

    private static string ComputeLegacyHashExternally(AuditLog log)
    {
        var userIdString = log.UserId?.Value.ToString() ?? "null";
        var data = $"{userIdString}|{log.EventType}|{log.Action}|{log.Details}|{log.IpAddress}|{log.CreatedAt:O}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
