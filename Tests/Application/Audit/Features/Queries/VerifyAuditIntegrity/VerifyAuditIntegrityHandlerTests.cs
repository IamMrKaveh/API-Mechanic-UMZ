using System.Reflection;
using Application.Audit.Features.Queries.VerifyAuditIntegrity;
using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.Audit.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Application.Audit.Features.Queries.VerifyAuditIntegrity;

public class VerifyAuditIntegrityHandlerTests
{
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly VerifyAuditIntegrityHandler _sut;

    public VerifyAuditIntegrityHandlerTests()
    {
        _sut = new VerifyAuditIntegrityHandler(_auditRepository);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ThrowsDomainException()
    {
        var query = new VerifyAuditIntegrityQuery(Guid.Empty);

        await Should.ThrowAsync<DomainException>(() => _sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenAuditLogNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns((AuditLog?)null);

        var query = new VerifyAuditIntegrityQuery(id);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCode.NotFound);
        result.Error.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WhenAuditLogNotFound_CallsRepositoryWithMappedAuditLogId()
    {
        var id = Guid.NewGuid();
        AuditLogId? capturedId = null;

        _auditRepository
            .GetByIdAsync(Arg.Do<AuditLogId>(x => capturedId = x), Arg.Any<CancellationToken>())
            .Returns((AuditLog?)null);

        var query = new VerifyAuditIntegrityQuery(id);

        await _sut.Handle(query, CancellationToken.None);

        capturedId.ShouldNotBeNull();
        capturedId!.Value.ShouldBe(id);
    }

    [Fact]
    public async Task Handle_WhenIntegrityValid_ReturnsSuccessWithIsValidTrue()
    {
        var log = new AuditLogBuilder()
            .WithRandomUser()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("127.0.0.1")
            .Build();

        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        var before = DateTime.UtcNow;
        var result = await _sut.Handle(query, CancellationToken.None);
        var after = DateTime.UtcNow;

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(log.Id.Value);
        result.Value.IsValid.ShouldBeTrue();
        result.Value.StoredHash.ShouldBe(log.IntegrityHash);
        result.Value.ExpectedHash.ShouldBe(log.IntegrityHash);
        result.Value.ExpectedHash.ShouldBe(result.Value.StoredHash);
        result.Value.VerifiedAt.ShouldBeGreaterThanOrEqualTo(before);
        result.Value.VerifiedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task Handle_WhenStoredHashIsTampered_ReturnsSuccessWithIsValidFalse()
    {
        var log = new AuditLogBuilder()
            .WithRandomUser()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("127.0.0.1")
            .Build();

        var originalExpected = log.RecomputeIntegrityHash();
        SetPrivateProperty(log, nameof(AuditLog.IntegrityHash), "tampered-hash-value");

        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(log.Id.Value);
        result.Value.IsValid.ShouldBeFalse();
        result.Value.StoredHash.ShouldBe("tampered-hash-value");
        result.Value.ExpectedHash.ShouldBe(originalExpected);
        result.Value.ExpectedHash.ShouldNotBe(result.Value.StoredHash);
    }

    [Fact]
    public async Task Handle_ExpectedHashUsesRecomputeIntegrityHash_NotStoredHash()
    {
        var log = new AuditLogBuilder()
            .WithRandomUser()
            .WithEventType("Order")
            .WithAction("Created")
            .WithIpAddress("10.0.0.1")
            .Build();

        var expectedHashFromDomain = log.RecomputeIntegrityHash();

        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ExpectedHash.ShouldBe(expectedHashFromDomain);
    }

    [Fact]
    public async Task Handle_VerifiedAtIsUtcAndCloseToNow()
    {
        var log = new AuditLogBuilder().WithRandomUser().Build();

        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = await _sut.Handle(query, CancellationToken.None);
        var after = DateTime.UtcNow.AddSeconds(1);

        result.ShouldBeSuccess();
        result.Value.VerifiedAt.ShouldBeGreaterThanOrEqualTo(before);
        result.Value.VerifiedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce_WithProvidedId()
    {
        var log = new AuditLogBuilder().WithRandomUser().Build();
        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        await _sut.Handle(query, CancellationToken.None);

        await _auditRepository
            .Received(1)
            .GetByIdAsync(
                Arg.Is<AuditLogId>(x => x == log.Id),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var log = new AuditLogBuilder().WithRandomUser().Build();

        _auditRepository
            .GetByIdAsync(Arg.Any<AuditLogId>(), Arg.Any<CancellationToken>())
            .Returns(log);

        var query = new VerifyAuditIntegrityQuery(log.Id.Value);

        await _sut.Handle(query, cts.Token);

        await _auditRepository
            .Received(1)
            .GetByIdAsync(Arg.Any<AuditLogId>(), cts.Token);
    }

    private static void SetPrivateProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull($"Property '{propertyName}' was not found on '{target.GetType().Name}'.");

        var setter = property!.GetSetMethod(nonPublic: true);
        setter.ShouldNotBeNull($"Property '{propertyName}' on '{target.GetType().Name}' has no accessible setter.");

        setter!.Invoke(target, new[] { value });
    }
}
