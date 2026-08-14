using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Repositories;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Auth.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class SessionRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private SessionRepository _sut = null!;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new SessionRepository(_context);

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [SkippableFact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var userId = UserId.NewId();
        var refreshToken = RefreshToken.Generate();
        var deviceInfo = DeviceInfo.Create("chrome-mac");
        var ipAddress = IpAddress.Create("10.0.0.1");

        var session = new UserSessionBuilder()
            .WithUserId(userId)
            .WithRefreshToken(refreshToken)
            .WithDeviceInfo(deviceInfo)
            .WithIpAddress(ipAddress)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(session.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(session.Id);
        loaded.UserId.ShouldBe(userId);
        loaded.RefreshToken.Value.ShouldBe(refreshToken.Value);
        loaded.DeviceInfo.Value.ShouldBe("chrome-mac");
        loaded.IpAddress.Value.ShouldBe("10.0.0.1");
        loaded.IsRevoked.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task GetByRefreshTokenAsync_WhenExists_ReturnsMatchingSession()
    {
        var refreshToken = RefreshToken.Generate();
        var session = new UserSessionBuilder()
            .WithRefreshToken(refreshToken)
            .Build();

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByRefreshTokenAsync(refreshToken);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(session.Id);
    }

    [SkippableFact]
    public async Task GetByRefreshTokenAsync_WhenNotExists_ReturnsNull()
    {
        var loaded = await _sut.GetByRefreshTokenAsync(RefreshToken.Generate());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyNonRevokedNonExpiredSessions()
    {
        var userId = UserId.NewId();

        var active1 = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo("device-a")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        var active2 = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo("device-b")
            .WithExpiresAt(DateTime.UtcNow.AddDays(3))
            .Build();

        var revoked = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo("device-c")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();
        revoked.Revoke(SessionRevocationReason.UserRequested);

        await _sut.AddAsync(active1);
        await _sut.AddAsync(active2);
        await _sut.AddAsync(revoked);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var actives = await _sut.GetActiveByUserIdAsync(userId);

        actives.Count.ShouldBe(2);
        actives.ShouldAllBe(s => !s.IsRevoked);
        actives.Select(s => s.Id).ShouldContain(active1.Id);
        actives.Select(s => s.Id).ShouldContain(active2.Id);
    }

    [SkippableFact]
    public async Task GetActiveByUserIdAsync_OrdersByCreatedAtDescending()
    {
        var userId = UserId.NewId();

        var first = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo("device-first")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();

        await Task.Delay(20);

        var second = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo("device-second")
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        await _sut.AddAsync(second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var actives = await _sut.GetActiveByUserIdAsync(userId);

        actives.Count.ShouldBe(2);
        actives[0].Id.ShouldBe(second.Id);
        actives[1].Id.ShouldBe(first.Id);
    }

    [SkippableFact]
    public async Task GetActiveByUserAndDeviceAsync_WhenActiveMatchExists_ReturnsIt()
    {
        var userId = UserId.NewId();
        var deviceInfo = DeviceInfo.Create("target-device");

        var session = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(deviceInfo)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByUserAndDeviceAsync(userId, deviceInfo);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(session.Id);
    }

    [SkippableFact]
    public async Task GetActiveByUserAndDeviceAsync_WhenOnlyRevokedMatchExists_ReturnsNull()
    {
        var userId = UserId.NewId();
        var deviceInfo = DeviceInfo.Create("revoked-device");

        var session = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(deviceInfo)
            .WithExpiresAt(DateTime.UtcNow.AddDays(7))
            .Build();
        session.Revoke(SessionRevocationReason.UserRequested);

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByUserAndDeviceAsync(userId, deviceInfo);

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetExpiredActiveSessionsAsync_ReturnsSessionsWhoseExpiresAtIsBeforeCutoff()
    {
        var soonExpiring = new UserSessionBuilder()
            .WithDeviceInfo("soon")
            .WithExpiresAt(DateTime.UtcNow.AddMinutes(1))
            .Build();

        var farFuture = new UserSessionBuilder()
            .WithDeviceInfo("far")
            .WithExpiresAt(DateTime.UtcNow.AddDays(30))
            .Build();

        await _sut.AddAsync(soonExpiring);
        await _sut.AddAsync(farFuture);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow.AddHours(1);
        var expired = await _sut.GetExpiredActiveSessionsAsync(cutoff);

        expired.Count.ShouldBe(1);
        expired[0].Id.ShouldBe(soonExpiring.Id);
    }

    [SkippableFact]
    public async Task GetExpiredActiveSessionsAsync_ExcludesAlreadyRevokedSessions()
    {
        var revoked = new UserSessionBuilder()
            .WithDeviceInfo("revoked")
            .WithExpiresAt(DateTime.UtcNow.AddMinutes(1))
            .Build();
        revoked.Revoke(SessionRevocationReason.UserRequested);

        await _sut.AddAsync(revoked);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var expired = await _sut.GetExpiredActiveSessionsAsync(DateTime.UtcNow.AddHours(1));

        expired.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task RevokeAllByUserIdAsync_MarksAllActiveSessionsForUserAsRevoked()
    {
        var userId = UserId.NewId();
        var otherUserId = UserId.NewId();

        var target1 = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("d1").Build();
        var target2 = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("d2").Build();
        var untouched = new UserSessionBuilder().WithUserId(otherUserId).WithDeviceInfo("d3").Build();

        await _sut.AddAsync(target1);
        await _sut.AddAsync(target2);
        await _sut.AddAsync(untouched);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.RevokeAllByUserIdAsync(userId);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloadedTarget1 = await _sut.GetByIdAsync(target1.Id);
        var reloadedTarget2 = await _sut.GetByIdAsync(target2.Id);
        var reloadedUntouched = await _sut.GetByIdAsync(untouched.Id);

        reloadedTarget1!.IsRevoked.ShouldBeTrue();
        reloadedTarget1.RevocationReason.ShouldBe(SessionRevocationReason.AllSessionsRevoked);
        reloadedTarget2!.IsRevoked.ShouldBeTrue();
        reloadedTarget2.RevocationReason.ShouldBe(SessionRevocationReason.AllSessionsRevoked);
        reloadedUntouched!.IsRevoked.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task RevokeAllByUserIdAsync_WithReason_UsesProvidedReason()
    {
        var userId = UserId.NewId();
        var session = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("d1").Build();

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.RevokeAllByUserIdAsync(userId, SessionRevocationReason.PasswordChanged);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(session.Id);

        reloaded!.IsRevoked.ShouldBeTrue();
        reloaded.RevocationReason.ShouldBe(SessionRevocationReason.PasswordChanged);
    }

    [SkippableFact]
    public async Task RevokeAllExceptAsync_LeavesSpecifiedSessionActiveAndRevokesTheRest()
    {
        var userId = UserId.NewId();
        var keep = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("keep").Build();
        var revoke1 = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("revoke1").Build();
        var revoke2 = new UserSessionBuilder().WithUserId(userId).WithDeviceInfo("revoke2").Build();

        await _sut.AddAsync(keep);
        await _sut.AddAsync(revoke1);
        await _sut.AddAsync(revoke2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.RevokeAllExceptAsync(userId, keep.Id, SessionRevocationReason.SecurityConcern);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloadedKeep = await _sut.GetByIdAsync(keep.Id);
        var reloadedRevoke1 = await _sut.GetByIdAsync(revoke1.Id);
        var reloadedRevoke2 = await _sut.GetByIdAsync(revoke2.Id);

        reloadedKeep!.IsRevoked.ShouldBeFalse();
        reloadedRevoke1!.IsRevoked.ShouldBeTrue();
        reloadedRevoke1.RevocationReason.ShouldBe(SessionRevocationReason.SecurityConcern);
        reloadedRevoke2!.IsRevoked.ShouldBeTrue();
        reloadedRevoke2.RevocationReason.ShouldBe(SessionRevocationReason.SecurityConcern);
    }

    [SkippableFact]
    public async Task RefreshTokenColumn_IsUnique_ThrowsWhenAttemptingDuplicateValue()
    {
        var duplicateToken = RefreshToken.Generate();

        var first = new UserSessionBuilder()
            .WithRefreshToken(duplicateToken)
            .WithDeviceInfo("device-first")
            .Build();

        var second = new UserSessionBuilder()
            .WithRefreshToken(duplicateToken)
            .WithDeviceInfo("device-second")
            .Build();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task RevocationReason_IsStoredAsString_RoundTripPreservesEnumValue()
    {
        var session = new UserSessionBuilder().WithDeviceInfo("d1").Build();
        session.Revoke(SessionRevocationReason.AdminRevoked);

        await _sut.AddAsync(session);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(session.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.IsRevoked.ShouldBeTrue();
        reloaded.RevocationReason.ShouldBe(SessionRevocationReason.AdminRevoked);
    }
}
