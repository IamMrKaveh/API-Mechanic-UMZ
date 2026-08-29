using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Auth.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OtpRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private OtpRepository _sut = null!;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new OtpRepository(_context);

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var userId = UserId.NewId();
        var otp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithCode("123456")
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(otp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(otp.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(otp.Id);
        loaded.UserId.ShouldBe(userId);
        loaded.Purpose.ShouldBe(OtpPurpose.Login);
        loaded.IsVerified.ShouldBeFalse();
        loaded.VerificationAttempts.ShouldBe(0);
        loaded.CodeHash.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(OtpId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestActiveByUserIdAsync_WhenMultipleActive_ReturnsMostRecentByCreatedAt()
    {
        var userId = UserId.NewId();

        var older = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(older);
        await _context.SaveChangesAsync();

        await Task.Delay(10);

        var newer = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(newer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var latest = await _sut.GetLatestActiveByUserIdAsync(userId, OtpPurpose.Login);

        latest.ShouldNotBeNull();
        latest!.Id.ShouldBe(newer.Id);
    }

    [Fact]
    public async Task GetLatestActiveByUserIdAsync_WhenLatestIsVerified_ReturnsNull()
    {
        var userId = UserId.NewId();
        var codeValue = "654321";
        var verified = new UserOtpBuilder()
            .WithUserId(userId)
            .WithCode(codeValue)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        verified.Verify(OtpCode.Create(codeValue));

        await _sut.AddAsync(verified);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var latest = await _sut.GetLatestActiveByUserIdAsync(userId, OtpPurpose.Login);

        latest.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestActiveByUserIdAsync_WhenLatestIsExpired_ReturnsNull()
    {
        var userId = UserId.NewId();

        var expired = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromTicks(1))
            .Build();

        await _sut.AddAsync(expired);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await Task.Delay(50);

        var latest = await _sut.GetLatestActiveByUserIdAsync(userId, OtpPurpose.Login);

        latest.ShouldBeNull();
    }

    [Fact]
    public async Task GetLatestActiveByUserIdAsync_FiltersByPurpose()
    {
        var userId = UserId.NewId();

        var loginOtp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        var passwordResetOtp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.PasswordReset)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(loginOtp);
        await _sut.AddAsync(passwordResetOtp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var latestLogin = await _sut.GetLatestActiveByUserIdAsync(userId, OtpPurpose.Login);
        var latestReset = await _sut.GetLatestActiveByUserIdAsync(userId, OtpPurpose.PasswordReset);

        latestLogin.ShouldNotBeNull();
        latestLogin!.Id.ShouldBe(loginOtp.Id);
        latestReset.ShouldNotBeNull();
        latestReset!.Id.ShouldBe(passwordResetOtp.Id);
    }

    [Fact]
    public async Task GetLatestActiveByUserIdAsync_FiltersByUserId()
    {
        var userA = UserId.NewId();
        var userB = UserId.NewId();

        var otpA = new UserOtpBuilder()
            .WithUserId(userA)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        var otpB = new UserOtpBuilder()
            .WithUserId(userB)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(otpA);
        await _sut.AddAsync(otpB);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var latestA = await _sut.GetLatestActiveByUserIdAsync(userA, OtpPurpose.Login);

        latestA.ShouldNotBeNull();
        latestA!.Id.ShouldBe(otpA.Id);
        latestA.UserId.ShouldBe(userA);
    }

    [Fact]
    public async Task CountRecentByUserIdAsync_CountsOnlyEntriesWithinWindow()
    {
        var userId = UserId.NewId();

        for (var i = 0; i < 3; i++)
        {
            var otp = new UserOtpBuilder()
                .WithUserId(userId)
                .WithPurpose(OtpPurpose.Login)
                .WithValidity(TimeSpan.FromMinutes(5))
                .Build();

            await _sut.AddAsync(otp);
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var count = await _sut.CountRecentByUserIdAsync(userId, OtpPurpose.Login, TimeSpan.FromMinutes(10));

        count.ShouldBe(3);
    }

    [Fact]
    public async Task CountRecentByUserIdAsync_FiltersByPurpose()
    {
        var userId = UserId.NewId();

        var login = new UserOtpBuilder().WithUserId(userId).WithPurpose(OtpPurpose.Login).WithValidity(TimeSpan.FromMinutes(5)).Build();
        var reset = new UserOtpBuilder().WithUserId(userId).WithPurpose(OtpPurpose.PasswordReset).WithValidity(TimeSpan.FromMinutes(5)).Build();

        await _sut.AddAsync(login);
        await _sut.AddAsync(reset);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loginCount = await _sut.CountRecentByUserIdAsync(userId, OtpPurpose.Login, TimeSpan.FromMinutes(10));
        var resetCount = await _sut.CountRecentByUserIdAsync(userId, OtpPurpose.PasswordReset, TimeSpan.FromMinutes(10));

        loginCount.ShouldBe(1);
        resetCount.ShouldBe(1);
    }

    [Fact]
    public async Task CountRecentByUserIdAsync_WhenNoRecentEntries_ReturnsZero()
    {
        var userId = UserId.NewId();

        var count = await _sut.CountRecentByUserIdAsync(userId, OtpPurpose.Login, TimeSpan.FromMinutes(10));

        count.ShouldBe(0);
    }

    [Fact]
    public async Task Update_AfterVerifyingOtp_PersistsVerifiedStateAndAttempts()
    {
        var userId = UserId.NewId();
        var codeValue = "246810";
        var otp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithCode(codeValue)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(otp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(otp.Id);
        loaded.ShouldNotBeNull();
        loaded!.Verify(OtpCode.Create(codeValue));

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(otp.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.IsVerified.ShouldBeTrue();
        reloaded.VerifiedAt.ShouldNotBeNull();
        reloaded.VerificationAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task PurposeIsStoredAsString_RoundTripPreservesEnumValue()
    {
        var userId = UserId.NewId();
        var otp = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.TwoFactorAuthentication)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();

        await _sut.AddAsync(otp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(otp.Id);

        loaded.ShouldNotBeNull();
        loaded!.Purpose.ShouldBe(OtpPurpose.TwoFactorAuthentication);
    }
}
