using Application.Auth.Contracts;
using Application.Wallet.Features.Commands.InitiateWalletTransfer;
using Application.Wallet.Options;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;
using SharedKernel.Abstractions.Interfaces;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed class InitiateWalletTransferHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IWalletTransferRepository _transferRepository = Substitute.For<IWalletTransferRepository>();
    private readonly IOtpService _otpService = Substitute.For<IOtpService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IOptions<WalletTransferOptions> _options = Options.Create(new WalletTransferOptions());

    private readonly InitiateWalletTransferHandler _sut;

    public InitiateWalletTransferHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _otpService.HashOtp(Arg.Any<OtpCode>()).Returns("otp-hash");
        _otpService.SendOtpAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Success(true));

        _sut = new InitiateWalletTransferHandler(
            _userRepository, _walletRepository, _transferRepository, _otpService,
            _unitOfWork, _dateTimeProvider, _options, _currentUserService);
    }

    private Users BuildUser(PhoneNumber phone) =>
        new UserBuilder().WithPhoneNumber(phone).Build();

    [Fact]
    public async Task Handle_WhenSenderHasNoPhoneNumber_ReturnsFailure()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(new UserBuilder().WithPhoneNumber(null).Build());

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenRecipientNotFound_ReturnsNotFound()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRecipientIsSameAsSender_ReturnsFailure()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        var senderPhone = PhoneNumber.Create("09120000000");
        var sender = new UserBuilder().WithPhoneNumber(senderPhone).Build();
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(sender);
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>()).Returns(sender);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand(senderPhone.Value, 50_000m, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenSenderWalletNotFound_ReturnsNotFound()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAmountBelowMinimum_ReturnsFailure()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        var sender = BuildUser(PhoneNumber.Create("09120000000"));
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(sender);
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        var wallet = new WalletBuilder().WithOwnerId(senderId).Build();
        wallet.Credit(Money.Create(1_000_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 5_000m, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenInsufficientBalance_ReturnsFailure()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        var wallet = new WalletBuilder().WithOwnerId(senderId).Build();
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenValidAndOtpSent_ReturnsSuccessWithTransferId()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        var wallet = new WalletBuilder().WithOwnerId(senderId).Build();
        wallet.Credit(Money.Create(1_000_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(wallet);
        _transferRepository.SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);
        _transferRepository.CountRecentPendingByUserAsync(Arg.Any<UserId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, "test transfer"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.TransferId.ShouldNotBe(Guid.Empty);
        result.Value.OtpLength.ShouldBe(new WalletTransferOptions().OtpLength);
        await _transferRepository.Received(1).AddAsync(Arg.Any<WalletTransfer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOtpSendFails_MarksTransferFailedAndReturnsFailure()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        var wallet = new WalletBuilder().WithOwnerId(senderId).Build();
        wallet.Credit(Money.Create(1_000_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(wallet);
        _transferRepository.SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0m);
        _transferRepository.CountRecentPendingByUserAsync(Arg.Any<UserId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(0);
        _otpService.SendOtpAsync(Arg.Any<PhoneNumber>(), Arg.Any<OtpCode>(), Arg.Any<OtpPurpose>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Failure("sms provider down"));

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenTooManyPendingTransfers_ReturnsConflict()
    {
        var senderId = UserId.NewId();
        _currentUserService.UserId.Returns(senderId.Value);
        _userRepository.GetByIdAsync(senderId, Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09120000000")));
        _userRepository.GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(BuildUser(PhoneNumber.Create("09121111111")));
        var wallet = new WalletBuilder().WithOwnerId(senderId).Build();
        wallet.Credit(Money.Create(1_000_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        _walletRepository.GetByUserIdAsync(senderId, Arg.Any<CancellationToken>()).Returns(wallet);
        _transferRepository.SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0m);
        _transferRepository.CountRecentPendingByUserAsync(Arg.Any<UserId>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new WalletTransferOptions().MaxPendingTransfersPerHour);

        var result = await _sut.Handle(
            new InitiateWalletTransferCommand("09121111111", 50_000m, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }
}
