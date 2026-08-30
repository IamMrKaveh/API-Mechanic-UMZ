using Application.Auth.Contracts;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Exceptions;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using RefreshTokens = Domain.Security.ValueObjects.RefreshToken;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.Auth.Features.Commands.VerifyOtp;

public class VerifyOtpHandlerTests
{
    private const string ValidCode = "135790";
    private const string WrongCode = "246801";

    private readonly IOtpRepository _otpRepository = Substitute.For<IOtpRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly DateTime _now = new(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);
    private readonly VerifyOtpHandler _sut;

    public VerifyOtpHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_now);

        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = "test-signing-key-with-at-least-32-characters-length",
            Issuer = "mechanic-tests-issuer",
            Audience = "mechanic-tests-audience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        });

        _sut = new VerifyOtpHandler(
            _otpRepository,
            _userRepository,
            _sessionService,
            _jwtTokenGenerator,
            _currentUser,
            _dateTimeProvider,
            jwtOptions);
    }

    private static Users BuildUserWithPhone(PhoneNumber phoneNumber) =>
        new UserBuilder().WithPhoneNumber(phoneNumber).Build();

    private static UserOtp BuildOtp(Guid userId, string code) =>
        new UserOtpBuilder()
            .WithUserId(UserId.From(userId))
            .WithCode(code)
            .WithPurpose(OtpPurpose.Login)
            .Build();

    private void SetupSuccessfulSession(Guid userId)
    {
        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(Guid.NewGuid(), RefreshTokens.Generate().Value, DateTime.UtcNow.AddDays(30), userId)));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("generated-access-token");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailureAndDoesNotQueryOtp()
    {
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var command = new VerifyOtpCommand("09123456789", ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _otpRepository.DidNotReceiveWithAnyArgs()
            .GetLatestActiveByUserIdAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenNoActiveOtpExists_ReturnsFailureAndDoesNotCreateSession()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns((UserOtp?)null);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _sessionService.DidNotReceiveWithAnyArgs()
            .CreateSessionAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCodeIsInvalid_ReturnsFailureUpdatesOtpAndDoesNotCreateSession()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var command = new VerifyOtpCommand(phoneNumber.Value, WrongCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        otp.VerificationAttempts.ShouldBe(1);
        _otpRepository.Received(1).Update(otp);
        await _sessionService.DidNotReceiveWithAnyArgs()
            .CreateSessionAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenOtpAlreadyVerified_ReturnsFailureAndUpdatesOtp()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);
        otp.Verify(OtpCode.Create(ValidCode));

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _otpRepository.Received(1).Update(otp);
        await _sessionService.DidNotReceiveWithAnyArgs()
            .CreateSessionAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenOtpExpired_ReturnsFailureAndUpdatesOtp()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = new UserOtpBuilder()
            .WithUserId(user.Id)
            .WithCode(ValidCode)
            .WithValidity(TimeSpan.FromMilliseconds(10))
            .Build();

        await Task.Delay(100);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _otpRepository.Received(1).Update(otp);
        await _sessionService.DidNotReceiveWithAnyArgs()
            .CreateSessionAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenMaxVerificationAttemptsExceeded_ReturnsFailureAndUpdatesOtp()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);
        var wrong = OtpCode.Create(WrongCode);

        for (var i = 0; i < 5; i++)
            Should.Throw<InvalidOtpCodeException>(() => otp.Verify(wrong));

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _otpRepository.Received(1).Update(otp);
        await _sessionService.DidNotReceiveWithAnyArgs()
            .CreateSessionAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCodeIsValid_VerifiesOtpAndUpdatesRepositoryExactlyOnce()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        SetupSuccessfulSession(user.Id.Value);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        otp.IsVerified.ShouldBeTrue();
        _otpRepository.Received(1).Update(otp);
    }

    [Fact]
    public async Task Handle_WhenDeviceInfoProvidedInRequest_UsesRequestDeviceInfoForSession()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        _currentUser.UserAgent.Returns("fallback-agent-should-not-be-used");

        SetupSuccessfulSession(user.Id.Value);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode, "Custom-Device-Descriptor");

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Any<IpAddress>(),
            "Custom-Device-Descriptor",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenDeviceInfoNotProvided_FallsBackToCurrentUserAgent()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        _currentUser.UserAgent.Returns("agent-from-current-user");

        SetupSuccessfulSession(user.Id.Value);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode, DeviceInfo: null);

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Any<IpAddress>(),
            "agent-from-current-user",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIpAddressIsMissing_UsesUnknownIpForSessionCreation()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        _currentUser.IpAddress.Returns((string?)null);

        SetupSuccessfulSession(user.Id.Value);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Is<IpAddress>(ip => ip == IpAddress.Unknown),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionCreationFails_ReturnsFailureWithSameErrorButOtpAlreadyUpdated()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Unauthorized("جلسه ایجاد نشد."));

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        _otpRepository.Received(1).Update(otp);
        _jwtTokenGenerator.DidNotReceiveWithAnyArgs().GenerateAccessToken(default!, default!);
    }

    [Fact]
    public async Task Handle_WhenAllSucceeds_GeneratesAccessTokenWithCorrectUserAndSessionId()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var newSessionId = Guid.NewGuid();

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(newSessionId, RefreshTokens.Generate().Value, DateTime.UtcNow.AddDays(30), user.Id.Value)));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("access-token-value");

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        await _sut.Handle(command, CancellationToken.None);

        _jwtTokenGenerator.Received(1).GenerateAccessToken(
            Arg.Is<Users>(u => u == user),
            Arg.Is<SessionId>(s => s!.Value == newSessionId));
    }

    [Fact]
    public async Task Handle_WhenAllSucceeds_ReturnsAuthResultWithMappedTokenFields()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = BuildOtp(user.Id.Value, ValidCode);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        var refreshExpiresAt = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newRefreshTokenValue = RefreshTokens.Generate().Value;

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(
                new RefreshTokenResult(Guid.NewGuid(), newRefreshTokenValue, refreshExpiresAt, user.Id.Value)));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("final-access-token");

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.AccessToken.ShouldBe("final-access-token");
        result.Value.RefreshToken.ShouldBe(newRefreshTokenValue);
        result.Value.RefreshTokenExpiresAt.ShouldBe(refreshExpiresAt);
        result.Value.AccessTokenExpiresAt.ShouldBe(_now.AddMinutes(60));
        result.Value.User.ShouldNotBeNull();
        result.Value.IsNewUser.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_PassesRequestedPurposeToOtpRepositoryLookup()
    {
        var phoneNumber = PhoneNumber.Create("09123456789");
        var user = BuildUserWithPhone(phoneNumber);
        var otp = new UserOtpBuilder()
            .WithUserId(user.Id)
            .WithCode(ValidCode)
            .WithPurpose(OtpPurpose.TwoFactorAuthentication)
            .Build();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _otpRepository
            .GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(otp);

        SetupSuccessfulSession(user.Id.Value);

        var command = new VerifyOtpCommand(phoneNumber.Value, ValidCode, Purpose: OtpPurpose.TwoFactorAuthentication);

        await _sut.Handle(command, CancellationToken.None);

        await _otpRepository.Received(1).GetLatestActiveByUserIdAsync(
            Arg.Is<UserId>(id => id == user.Id),
            OtpPurpose.TwoFactorAuthentication,
            Arg.Any<CancellationToken>());
    }
}
