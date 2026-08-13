using Application.Auth.Contracts;
using Application.Auth.Features.Commands.SendOtp;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.Auth.Features.Commands.SendOtp;

public class SendOtpHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly IOtpService _otpService = Substitute.For<IOtpService>(); private readonly IOtpRepository _otpRepository = Substitute.For<IOtpRepository>(); private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly IInitialAdminOptions _initialAdminOptions = Substitute.For<IInitialAdminOptions>(); private readonly SendOtpHandler _sut;

    public SendOtpHandlerTests()
    {
        _initialAdminOptions.PhoneNumbers.Returns(new List<string>());
        _sut = new SendOtpHandler(_unitOfWork, _otpService, _otpRepository, _userRepository, _initialAdminOptions);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_RegistersNewUserByPhone()
    {
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);
        _otpService
            .ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new SendOtpCommand("09123456789", OtpPurpose.Login);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userRepository.Received(1).AddAsync(
            Arg.Is<Users>(u => u!.PhoneNumber != null && u.PhoneNumber.Value == "09123456789" && u.IsAdmin == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExistAndPhoneIsInitialAdmin_PromotesNewUserToAdmin()
    {
        _initialAdminOptions.PhoneNumbers.Returns(new List<string> { "09123456789" });
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);
        _otpService
            .ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new SendOtpCommand("09123456789", OtpPurpose.Login);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userRepository.Received(1).AddAsync(
            Arg.Is<Users>(u => u.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserExists_DoesNotAddNewUser()
    {
        var existingUser = Users.RegisterByPhone(PhoneNumber.Create("09123456789"));
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _otpService
            .ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new SendOtpCommand("09123456789", OtpPurpose.Login);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenRateLimitExceeded_ReturnsFailureAndDoesNotAddOtp()
    {
        var existingUser = Users.RegisterByPhone(PhoneNumber.Create("09123456789"));
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _otpService
            .ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new SendOtpCommand("09123456789", OtpPurpose.Login);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _otpRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenRateLimitPasses_AddsOtpAndSavesChanges()
    {
        var existingUser = Users.RegisterByPhone(PhoneNumber.Create("09123456789"));
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);
        _otpService
            .ValidateRateLimitAsync(Arg.Any<UserId>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new SendOtpCommand("09123456789", OtpPurpose.Login);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _otpRepository.Received(1).AddAsync(
            Arg.Is<UserOtp>(o => o.UserId == existingUser.Id && o.Purpose == OtpPurpose.Login),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
