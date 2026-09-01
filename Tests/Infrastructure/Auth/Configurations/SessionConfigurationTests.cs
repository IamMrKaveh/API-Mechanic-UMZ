using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ValueObjects;
using Shouldly;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Xunit;

namespace Tests.Infrastructure.Auth.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class SessionConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task Persist_NewUserSession_RoundTripsAllMappedProperties()
    {
        var userId = UserId.NewId();
        var refreshToken = RefreshToken.Generate();
        var deviceInfo = DeviceInfo.Create("Mozilla/5.0 iPhone");
        var ipAddress = IpAddress.Create("192.168.1.10");
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var session = new UserSessionBuilder()
            .WithUserId(userId)
            .WithRefreshToken(refreshToken)
            .WithDeviceInfo(deviceInfo)
            .WithIpAddress(ipAddress)
            .WithExpiresAt(expiresAt)
            .Build();
        session.ClearDomainEvents();

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.UserSessions.SingleAsync(s => s.Id == session.Id);

        reloaded.UserId.ShouldBe(userId);
        reloaded.RefreshToken.Value.ShouldBe(refreshToken.Value);
        reloaded.DeviceInfo.Value.ShouldBe(deviceInfo.Value);
        reloaded.IpAddress.Value.ShouldBe(ipAddress.Value);
        reloaded.ExpiresAt.ShouldBe(expiresAt, TimeSpan.FromMilliseconds(1));
        reloaded.IsRevoked.ShouldBeFalse();
        reloaded.RevocationReason.ShouldBeNull();
        reloaded.RevokedAt.ShouldBeNull();
        reloaded.LastActivityAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_DuplicateRefreshToken_ThrowsDbUpdateException()
    {
        var sharedToken = RefreshToken.Generate();

        var first = new UserSessionBuilder().WithRefreshToken(sharedToken).Build();
        first.ClearDomainEvents();

        var second = new UserSessionBuilder().WithRefreshToken(sharedToken).Build();
        second.ClearDomainEvents();

        await _context.UserSessions.AddAsync(first);
        await _context.SaveChangesAsync();

        await _context.UserSessions.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () =>
            await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_SameUserAndDeviceWhileActive_ThrowsDbUpdateException()
    {
        var userId = UserId.NewId();
        var device = DeviceInfo.Create("Chrome/Windows");

        var first = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(device)
            .Build();
        first.ClearDomainEvents();

        var second = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(device)
            .Build();
        second.ClearDomainEvents();

        await _context.UserSessions.AddAsync(first);
        await _context.SaveChangesAsync();

        await _context.UserSessions.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () =>
            await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_SameUserAndDeviceAfterRevocation_Succeeds()
    {
        var userId = UserId.NewId();
        var device = DeviceInfo.Create("Firefox/Linux");

        var first = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(device)
            .Build();
        first.ClearDomainEvents();

        await _context.UserSessions.AddAsync(first);
        await _context.SaveChangesAsync();

        first.Revoke(SessionRevocationReason.UserRequested);
        first.ClearDomainEvents();
        await _context.SaveChangesAsync();

        var second = new UserSessionBuilder()
            .WithUserId(userId)
            .WithDeviceInfo(device)
            .Build();
        second.ClearDomainEvents();

        await _context.UserSessions.AddAsync(second);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var count = await freshContext.UserSessions
            .CountAsync(s => s.UserId == userId);

        count.ShouldBe(2);
    }

    [Theory]
    [InlineData(SessionRevocationReason.UserRequested)]
    [InlineData(SessionRevocationReason.AdminRevoked)]
    [InlineData(SessionRevocationReason.SecurityConcern)]
    [InlineData(SessionRevocationReason.PasswordChanged)]
    [InlineData(SessionRevocationReason.AccountDeactivated)]
    [InlineData(SessionRevocationReason.Expired)]
    [InlineData(SessionRevocationReason.AllSessionsRevoked)]
    [InlineData(SessionRevocationReason.PhoneChanged)]
    public async Task Persist_RevocationReason_IsStoredAsStringConversion(SessionRevocationReason reason)
    {
        var session = new UserSessionBuilder().Build();
        session.ClearDomainEvents();

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        session.Revoke(reason);
        session.ClearDomainEvents();
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var reloaded = await freshContext.UserSessions.SingleAsync(s => s.Id == session.Id);
        reloaded.RevocationReason.ShouldBe(reason);

        var rawReason = await freshContext.Database
            .SqlQueryRaw<string>(
                "SELECT \"RevocationReason\" AS \"Value\" FROM \"UserSessions\" WHERE \"Id\" = {0}",
                session.Id.Value)
            .SingleAsync();

        rawReason.ShouldBe(reason.ToString());
    }

    [Fact]
    public async Task Persist_UserSession_HasRowVersionShadowPropertyThatChangesOnUpdate()
    {
        var session = new UserSessionBuilder().Build();
        session.ClearDomainEvents();

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        var initialRowVersion = _context.Entry(session).Property<byte[]>("RowVersion").CurrentValue;
        initialRowVersion.ShouldNotBeNull();

        session.UpdateActivity(DateTime.UtcNow.AddMinutes(1));
        await _context.SaveChangesAsync();

        var updatedRowVersion = _context.Entry(session).Property<byte[]>("RowVersion").CurrentValue;
        updatedRowVersion.ShouldNotBeNull();
        updatedRowVersion.ShouldNotBe(initialRowVersion);
    }

    [Fact]
    public async Task Query_UserSessionByRefreshToken_ReturnsExpectedSession()
    {
        var token = RefreshToken.Generate();
        var session = new UserSessionBuilder().WithRefreshToken(token).Build();
        session.ClearDomainEvents();

        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var found = await freshContext.UserSessions
            .SingleOrDefaultAsync(s => s.RefreshToken == token);

        found.ShouldNotBeNull();
        found!.Id.ShouldBe(session.Id);
    }
}
