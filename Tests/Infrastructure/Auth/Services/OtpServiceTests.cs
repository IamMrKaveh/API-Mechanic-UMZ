using Application.Audit.Contracts;
using Application.Auth.Features.Shared;
using Application.Communication.Contracts;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Services;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Auth.Services;

public class OtpServiceTests
{
    private readonly IOtpRepository _otpRepository = Substitute.For<IOtpRepository>(); private readonly ISmsService _smsService = Substitute.For<ISmsService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly OtpService _sut;

    public OtpServiceTests()
    {
        var options = Options.Create(new AuthOptions
        {
            OtpRateLimitWindowMinutes = 10,
            MaxOtpPerWindow = 3,
        });

        _sut = new OtpService(_otpRepository, _smsService, options, _auditService);
    }

    [Fact]
    public void HashOtp_ForSameCode_ProducesIdenticalHash()
    {
        var code = OtpCode.Create("123456");

        var first = _sut.HashOtp(code);
        var second = _sut.HashOtp(code);

        first.ShouldBe(second);
        first.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("123456", "654321")]
    [InlineData("000000", "999999")]
    [InlineData("111111", "111112")]
    public void HashOtp_ForDifferentCodes_ProducesDifferentHashes(string firstCode, string secondCode)
    {
        var hashA = _sut.HashOtp(OtpCode.Create(firstCode));
        var hashB = _sut.HashOtp(OtpCode.Create(secondCode));

        hashA.ShouldNotBe(hashB);
    }

    [Fact]
    public async Task SendOtpAsync_WhenSmsServiceReturnsTrue_ReturnsSuccess()
    {
        var phone = new PhoneNumberBuilder().Build();
        var code = OtpCode.Create("123456");

        _smsService
            .SendOtpSMSAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.SendOtpAsync(phone, code, OtpPurpose.Login);

        result.ShouldBeSuccess();
        result.Value.ShouldBeTrue();
        await _smsService.Received(1).SendOtpSMSAsync(phone, code, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpAsync_WhenSmsServiceReturnsFalse_ReturnsFailure()
    {
        var phone = new PhoneNumberBuilder().Build();
        var code = OtpCode.Create("123456");

        _smsService
            .SendOtpSMSAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.SendOtpAsync(phone, code, OtpPurpose.Login);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task SendOtpAsync_WhenSmsServiceThrows_LogsSystemEventAndReturnsFailure()
    {
        var phone = new PhoneNumberBuilder().WithValue("09121234567").Build();
        var code = OtpCode.Create("123456");

        _smsService
            .SendOtpSMSAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("sms provider down"));

        var result = await _sut.SendOtpAsync(phone, code, OtpPurpose.Login);

        result.IsFailure.ShouldBeTrue();
        await _auditService.Received(1).LogSystemEventAsync(
            "SendOtpFailed",
            Arg.Is<string>(details => details!.Contains("09121234567") && details!.Contains("sms provider down")),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task ValidateRateLimitAsync_WhenCountBelowMax_ReturnsTrue(int recentCount, bool expected)
    {
        var userId = UserId.NewId();
        _otpRepository
            .CountRecentByUserIdAsync(userId, OtpPurpose.Login, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(recentCount);

        var result = await _sut.ValidateRateLimitAsync(userId, OtpPurpose.Login);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    public async Task ValidateRateLimitAsync_WhenCountAtOrAboveMax_ReturnsFalse(int recentCount)
    {
        var userId = UserId.NewId();
        _otpRepository
            .CountRecentByUserIdAsync(userId, OtpPurpose.Login, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(recentCount);

        var result = await _sut.ValidateRateLimitAsync(userId, OtpPurpose.Login);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateRateLimitAsync_UsesConfiguredWindowMinutes()
    {
        var userId = UserId.NewId();
        _otpRepository
            .CountRecentByUserIdAsync(userId, OtpPurpose.Login, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        await _sut.ValidateRateLimitAsync(userId, OtpPurpose.Login);

        await _otpRepository.Received(1).CountRecentByUserIdAsync(
            userId,
            OtpPurpose.Login,
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
