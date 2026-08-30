using Application.Auth.Contracts;
using Application.Wallet.Features.Commands.ConfirmWalletTransfer;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed class ConfirmWalletTransferHandlerTests
{
    private readonly IWalletTransferRepository _transferRepository = Substitute.For<IWalletTransferRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IOtpService _otpService = Substitute.For<IOtpService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();

    private readonly ConfirmWalletTransferHandler _sut;

    public ConfirmWalletTransferHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FakeLockHandle("wallet", true));
        _sut = new ConfirmWalletTransferHandler(
            _transferRepository, _walletRepository, _userRepository, _otpService,
            _unitOfWork, _auditService, _dateTimeProvider, _currentUserService, _distributedLock);
    }

    [Fact]
    public async Task Handle_WhenTransferNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>())
            .Returns((WalletTransfer?)null);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(Guid.NewGuid(), "123456"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotSender_ReturnsForbidden()
    {
        var otherUser = UserId.NewId();
        _currentUserService.UserId.Returns(otherUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(UserId.NewId()).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenLockNotAcquired_ReturnsConflict()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _distributedLock.AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((ILockHandle?)null);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenOtpCodeInvalid_ReturnsFailure()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "abcxyz"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenOtpMismatch_ReturnsFailure()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var correctHash = "expected-hash";
        var transfer = new WalletTransferBuilder()
            .FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).WithOtpHash(correctHash).Build();
        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _otpService.HashOtp(Arg.Any<OtpCode>()).Returns("wrong-hash");

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        transfer.OtpAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenValid_TransfersFundsAndReturnsSuccess()
    {
        var fromUser = UserId.NewId();
        var toUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var hash = "matching-hash";
        var transfer = new WalletTransferBuilder()
            .FromUser(fromUser).ToUser(toUser).WithAmount(50_000m).WithOtpHash(hash).Build();

        var senderWallet = new WalletBuilder().WithOwnerId(fromUser).Build();
        senderWallet.Credit(Money.Create(200_000m), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        var recipientWallet = new WalletBuilder().WithOwnerId(toUser).Build();
        var recipient = new UserBuilder().WithPhoneNumber(PhoneNumber.Create("09121234567")).Build();

        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _otpService.HashOtp(Arg.Any<OtpCode>()).Returns(hash);
        _walletRepository.GetByUserIdForUpdateAsync(fromUser, Arg.Any<CancellationToken>()).Returns(senderWallet);
        _walletRepository.GetByUserIdForUpdateAsync(toUser, Arg.Any<CancellationToken>()).Returns(recipientWallet);
        _userRepository.GetByIdAsync(toUser, Arg.Any<CancellationToken>()).Returns(recipient);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.ShouldBeSuccess();
        transfer.Status.ShouldBe(WalletTransferStatus.Completed);
        senderWallet.Balance.Amount.ShouldBe(150_000m);
        recipientWallet.Balance.Amount.ShouldBe(50_000m);
        await _unitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSenderWalletNotFound_MarksTransferFailedAndReturnsFailure()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var hash = "matching-hash";
        var transfer = new WalletTransferBuilder()
            .FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).WithOtpHash(hash).Build();

        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _otpService.HashOtp(Arg.Any<OtpCode>()).Returns(hash);
        _walletRepository.GetByUserIdForUpdateAsync(fromUser, Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        transfer.Status.ShouldBe(WalletTransferStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenTransferNotPendingOtp_ReturnsFailure()
    {
        var fromUser = UserId.NewId();
        _currentUserService.UserId.Returns(fromUser.Value);
        var transfer = new WalletTransferBuilder().FromUser(fromUser).ToUser(UserId.NewId()).WithAmount(50_000m).Build();
        transfer.Cancel(fromUser);

        _transferRepository.GetByIdAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);
        _transferRepository.GetByIdForUpdateAsync(Arg.Any<WalletTransferId>(), Arg.Any<CancellationToken>()).Returns(transfer);

        var result = await _sut.Handle(new ConfirmWalletTransferCommand(transfer.Id.Value, "123456"), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
