using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.User.Features.Commands.RequestPhoneChangeOtp;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using UserOtpAggregate = Domain.Security.Aggregates.UserOtp;

namespace Tests.Application.User.Features.Commands.RequestPhoneChangeOtp;

public class RequestPhoneChangeOtpHandlerTests : HandlerTestBase
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOtpRepository _otpRepository = Substitute.For<IOtpRepository>();
    private readonly IOtpService _otpService = Substitute.For<IOtpService>();
    private readonly RequestPhoneChangeOtpHandler _sut;
    private readonly Guid _userGuid = Guid.NewGuid();

    public RequestPhoneChangeOtpHandlerTests()
    {
        _sut = new RequestPhoneChangeOtpHandler(
            CurrentUserService,
            _userRepository,
            _otpService,
            _otpRepository,
            UnitOfWork,
            DateTimeProvider,
            Options.Create(new AuthOptions { OtpLength = 6, OtpExpirationMinutes = 5 }));

        CurrentUserService.UserId.Returns((Guid?)_userGuid);
        UseLiveUtcNow();
        _otpService.ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _otpService.SendOtpAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Success(true));
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        AuthenticateAsAnonymous();

        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("09123456789"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Unauthorized);
        await _otpRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenPhoneInvalid_ReturnsFailure()
    {
        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("not-a-phone"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _otpRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenPhoneBelongsToAnotherUser_ReturnsConflict()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("09123456789"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _otpRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenRateLimited_ReturnsFailure()
    {
        _otpService.ValidateRateLimitAsync(Arg.Any<UserId>(), OtpPurpose.PhoneVerification, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("09123456789"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _otpRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _otpService.DidNotReceiveWithAnyArgs()
            .SendOtpAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenValid_StoresTargetBoundOtpAndSendsToTheNewNumber()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("09123456789"), CancellationToken.None);

        result.ShouldBeSuccess();

        await _otpRepository.Received(1).AddAsync(
            Arg.Is<UserOtpAggregate>(o =>
                o.Purpose == OtpPurpose.PhoneVerification &&
                o.TargetPhoneNumber != null &&
                o.TargetPhoneNumber.Value == "09123456789" &&
                o.UserId.Value == _userGuid),
            Arg.Any<CancellationToken>());

        await _otpService.Received(1).SendOtpAsync(
            Arg.Is<PhoneNumber>(p => p.Value == "09123456789"),
            Arg.Any<OtpCode>(),
            OtpPurpose.PhoneVerification,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSendingFails_ReturnsFailureAndDoesNotSave()
    {
        _userRepository.ExistsByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _otpService.SendOtpAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Failure("ارسال پیامک ناموفق بود."));

        var result = await _sut.Handle(new RequestPhoneChangeOtpCommand("09123456789"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
