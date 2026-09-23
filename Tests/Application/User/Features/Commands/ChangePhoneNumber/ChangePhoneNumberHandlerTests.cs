using Application.Audit.Contracts;
using Application.User.Features.Commands.ChangePhoneNumber;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using UserAggregate = Domain.User.Aggregates.User;
using UserOtpAggregate = Domain.Security.Aggregates.UserOtp;

namespace Tests.Application.User.Features.Commands.ChangePhoneNumber;

public class ChangePhoneNumberHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOtpRepository _otpRepository = Substitute.For<IOtpRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ChangePhoneNumberHandler _sut;
    private readonly Guid _userGuid = Guid.NewGuid();

    public ChangePhoneNumberHandlerTests()
    {
        _sut = new ChangePhoneNumberHandler(_userRepository, _otpRepository, _currentUser, _auditService, _clock);
        _currentUser.UserId.Returns((Guid?)_userGuid);
        _clock.UtcNow.Returns(_ => DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenPhoneInvalid_ReturnsFailure()
    {
        var result = await _sut.Handle(new ChangePhoneNumberCommand("not-a-phone", "123456"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _otpRepository.DidNotReceiveWithAnyArgs().GetLatestActiveByUserIdAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenPhoneAlreadyRegistered_ReturnsConflict()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(new ChangePhoneNumberCommand("09123456789", "123456"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _otpRepository.DidNotReceiveWithAnyArgs().GetLatestActiveByUserIdAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var result = await _sut.Handle(new ChangePhoneNumberCommand("09123456789", "123456"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNoActiveOtp_ReturnsFailure()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09120000001")).Build());
        _otpRepository.GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), OtpPurpose.PhoneVerification, Arg.Any<CancellationToken>())
            .Returns((UserOtpAggregate?)null);

        var result = await _sut.Handle(new ChangePhoneNumberCommand("09123456789", "123456"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenOtpVerifyFails_UpdatesOtpAndReturnsFailure()
    {
        var user = new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09120000001")).Build();
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        var otp = new UserOtpBuilder()
            .WithUserId(UserId.From(_userGuid))
            .WithCode("123456")
            .WithPurpose(OtpPurpose.PhoneVerification)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();
        _otpRepository.GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), OtpPurpose.PhoneVerification, Arg.Any<CancellationToken>())
            .Returns(otp);

        var result = await _sut.Handle(new ChangePhoneNumberCommand("09123456789", "000000"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        _otpRepository.Received(1).Update(otp);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenValid_ChangesPhoneAuditsAndReturnsSuccess()
    {
        var user = new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09120000001")).Build();
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        var otp = new UserOtpBuilder()
            .WithUserId(UserId.From(_userGuid))
            .WithCode("123456")
            .WithPurpose(OtpPurpose.PhoneVerification)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build();
        _otpRepository.GetLatestActiveByUserIdAsync(Arg.Any<UserId>(), OtpPurpose.PhoneVerification, Arg.Any<CancellationToken>())
            .Returns(otp);

        var result = await _sut.Handle(new ChangePhoneNumberCommand("09123456789", "123456"), CancellationToken.None);

        result.ShouldBeSuccess();
        user.PhoneNumber!.Value.ShouldBe("09123456789");
        _userRepository.Received(1).Update(user);
        await _auditService.Received(1).LogSecurityEventAsync(
            "PhoneNumberChanged",
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Is<UserId>(u => u != null && u.Value == _userGuid),
            Arg.Any<CancellationToken>());
    }
}
