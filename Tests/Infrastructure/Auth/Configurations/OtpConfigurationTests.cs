using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Xunit;

namespace Tests.Infrastructure.Auth.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OtpConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
    public async Task Persist_NewUserOtp_RoundTripsAllMappedPropertiesIncludingCodeHash()
    {
        var userId = UserId.NewId();
        var code = OtpCode.Create("123456");
        var otp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithCode(code)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();
        otp.ClearDomainEvents();

        await _context.UserOtps.AddAsync(otp);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.UserOtps.SingleAsync(o => o.Id == otp.Id);

        reloaded.Id.ShouldBe(otp.Id);
        reloaded.UserId.ShouldBe(userId);
        reloaded.CodeHash.ShouldBe(code.ToHash());
        reloaded.Purpose.ShouldBe(OtpPurpose.Login);
        reloaded.IsVerified.ShouldBeFalse();
        reloaded.VerificationAttempts.ShouldBe(0);
        reloaded.VerifiedAt.ShouldBeNull();
        reloaded.ExpiresAt.ShouldBeGreaterThan(reloaded.CreatedAt);
    }

    [Theory]
    [InlineData(OtpPurpose.EmailVerification)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.PhoneVerification)]
    [InlineData(OtpPurpose.TwoFactorAuthentication)]
    [InlineData(OtpPurpose.Login)]
    public async Task Persist_UserOtpWithPurpose_StoresPurposeAsStringConversion(OtpPurpose purpose)
    {
        var otp = new UserOtpBuilder().WithPurpose(purpose).Build();
        otp.ClearDomainEvents();

        await _context.UserOtps.AddAsync(otp);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var reloaded = await freshContext.UserOtps.SingleAsync(o => o.Id == otp.Id);
        reloaded.Purpose.ShouldBe(purpose);

        var rawPurpose = await freshContext.Database
            .SqlQueryRaw<string>(
                "SELECT \"Purpose\" AS \"Value\" FROM \"UserOtps\" WHERE \"Id\" = {0}",
                otp.Id.Value)
            .SingleAsync();

        rawPurpose.ShouldBe(purpose.ToString());
    }

    [Fact]
    public async Task Persist_UserOtp_HasRowVersionShadowPropertyThatChangesOnUpdate()
    {
        var otp = new UserOtpBuilder()
            .WithCode("654321")
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();
        otp.ClearDomainEvents();

        await _context.UserOtps.AddAsync(otp);
        await _context.SaveChangesAsync();

        var initialRowVersion = _context.Entry(otp).Property<byte[]>("RowVersion").CurrentValue;
        initialRowVersion.ShouldNotBeNull();

        otp.Verify(OtpCode.Create("654321"));
        otp.ClearDomainEvents();
        await _context.SaveChangesAsync();

        var updatedRowVersion = _context.Entry(otp).Property<byte[]>("RowVersion").CurrentValue;
        updatedRowVersion.ShouldNotBeNull();
        updatedRowVersion.ShouldNotBe(initialRowVersion);
    }

    [Fact]
    public async Task Persist_UserOtp_VerifyPersistsIsVerifiedAndVerifiedAt()
    {
        var otp = new UserOtpBuilder()
            .WithCode("111111")
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();
        otp.ClearDomainEvents();

        await _context.UserOtps.AddAsync(otp);
        await _context.SaveChangesAsync();

        otp.Verify(OtpCode.Create("111111"));
        otp.ClearDomainEvents();
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.UserOtps.SingleAsync(o => o.Id == otp.Id);

        reloaded.IsVerified.ShouldBeTrue();
        reloaded.VerifiedAt.ShouldNotBeNull();
        reloaded.VerificationAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task Query_UserOtpByUserAndPurposeAndExpiry_ReturnsExpectedRow()
    {
        var userId = UserId.NewId();
        var loginOtp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(10))
            .Build();
        loginOtp.ClearDomainEvents();

        var resetOtp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.PasswordReset)
            .WithValidity(TimeSpan.FromMinutes(10))
            .Build();
        resetOtp.ClearDomainEvents();

        await _context.UserOtps.AddRangeAsync(loginOtp, resetOtp);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var now = DateTime.UtcNow;
        var results = await freshContext.UserOtps
            .Where(o => o.UserId == userId && o.Purpose == OtpPurpose.Login && o.ExpiresAt > now)
            .ToListAsync();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(loginOtp.Id);
    }
}
