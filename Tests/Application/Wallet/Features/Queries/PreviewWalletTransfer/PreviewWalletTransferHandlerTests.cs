using Application.Wallet.Features.Queries.PreviewWalletTransfer;
using Application.Wallet.Options;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using Microsoft.Extensions.Options;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Queries.PreviewWalletTransfer;

public class PreviewWalletTransferHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IWalletTransferRepository _transferRepository = Substitute.For<IWalletTransferRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly WalletTransferOptions _options = new()
    {
        MinimumAmount = 10_000m,
        MaximumAmount = 1_000_000_000m,
        DailyLimit = 50_000_000m,
        OtpLength = 6,
        OtpTtlSeconds = 180,
        MaxPendingTransfersPerHour = 5,
        Currency = "IRT"
    };

    private readonly PreviewWalletTransferHandler _sut;

    public PreviewWalletTransferHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        _sut = new PreviewWalletTransferHandler(
            _userRepository,
            _walletRepository,
            _transferRepository,
            Options.Create(_options),
            _currentUserService);
    }

    private static Users NewRecipient(string phone = "09121234567")
        => new UserBuilder().WithPhoneNumber(PhoneNumber.Create(phone)).Build();

    [Fact]
    public async Task Handle_WhenRecipientPhoneNumberIsInvalid_ReturnsFailureWithDomainMessage()
    {
        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("not-a-phone", 20_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldNotBeNullOrWhiteSpace();
        await _userRepository.DidNotReceiveWithAnyArgs()
            .GetByPhoneNumberAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenRecipientNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRecipientIsSameAsSender_ReturnsFailure()
    {
        var recipient = NewRecipient();
        _currentUserService.UserId.Returns((Guid?)recipient.Id.Value);

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain("خود");
    }

    [Fact]
    public async Task Handle_WhenRecipientIsInactive_ReturnsFailure()
    {
        var recipient = NewRecipient();
        recipient.Deactivate();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain("غیرفعال");
    }

    [Fact]
    public async Task Handle_WhenRecipientWalletIsFrozen_ReturnsFailure()
    {
        var recipient = NewRecipient();
        var recipientWallet = new WalletBuilder().WithOwnerId(recipient.Id).Build();
        recipientWallet.Freeze("suspicious", UserId.NewId());

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(null, recipientWallet);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldContain("مسدود");
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsSenderAvailableBalance_ReturnsPreviewWithWarningAndCannotProceed()
    {
        var recipient = NewRecipient();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(null, (Wallets?)null);

        _transferRepository
            .SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanProceed.ShouldBeFalse();
        result.Value.Warning.ShouldNotBeNull();
        result.Value.Warning!.ShouldContain("موجودی");
        result.Value.SenderAvailableBalance.ShouldBe(0m);
    }

    [Fact]
    public async Task Handle_WhenAmountExceedsRemainingDailyLimit_ReturnsPreviewWithDailyLimitWarning()
    {
        var recipient = NewRecipient();
        var senderId = _currentUserService.UserId!.Value;
        var senderWallet = new WalletBuilder().WithOwnerId(UserId.From(senderId)).Build();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(senderWallet, (Wallets?)null);

        _transferRepository
            .SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(_options.DailyLimit);

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanProceed.ShouldBeFalse();
        result.Value.RemainingDailyLimit.ShouldBe(0m);
        result.Value.Warning.ShouldNotBeNull();
        result.Value.Warning!.ShouldContain("سقف روزانه");
    }

    [Fact]
    public async Task Handle_WhenAmountBelowMinimumAmount_ReturnsPreviewWithMinimumAmountWarning()
    {
        var recipient = NewRecipient();
        var senderId = _currentUserService.UserId!.Value;
        var senderWallet = new WalletBuilder().WithOwnerId(UserId.From(senderId)).Build();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(senderWallet, (Wallets?)null);

        _transferRepository
            .SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var belowMin = _options.MinimumAmount - 1m;

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", belowMin),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanProceed.ShouldBeFalse();
        result.Value.Warning.ShouldNotBeNull();
        result.Value.Warning!.ShouldContain("حداقل");
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsPreviewWithCanProceedTrueAndNoWarning()
    {
        var recipient = NewRecipient();
        var senderId = _currentUserService.UserId!.Value;

        var senderWallet = new WalletBuilder().WithOwnerId(UserId.From(senderId)).Build();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);

        _walletRepository
            .GetByUserIdAsync(
                Arg.Is<UserId>(x => x.Value == senderId),
                Arg.Any<CancellationToken>())
            .Returns(senderWallet);
        _walletRepository
            .GetByUserIdAsync(
                Arg.Is<UserId>(x => x.Value == recipient.Id.Value),
                Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);

        _transferRepository
            .SumCompletedAmountForDayAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        senderWallet.Credit(
            Money.Create(1_000_000m, "IRT"),
            "seed",
            "seed-ref-" + Guid.NewGuid().ToString("N"),
            "idem-" + Guid.NewGuid().ToString("N"));

        var amount = 100_000m;

        var result = await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", amount),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanProceed.ShouldBeTrue();
        result.Value.Warning.ShouldBeNull();
        result.Value.RecipientUserId.ShouldBe(recipient.Id.Value);
        result.Value.Amount.ShouldBe(amount);
        result.Value.DailyLimit.ShouldBe(_options.DailyLimit);
        result.Value.AlreadyTransferredToday.ShouldBe(0m);
        result.Value.RemainingDailyLimit.ShouldBe(_options.DailyLimit);
        result.Value.SenderAvailableBalance.ShouldBe(1_000_000m);
        result.Value.RecipientPhoneMasked.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_UsesTodayInUtcAsDayForDailyLimitLookup()
    {
        var recipient = NewRecipient();
        var senderId = _currentUserService.UserId!.Value;
        var senderWallet = new WalletBuilder().WithOwnerId(UserId.From(senderId)).Build();

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);
        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(senderWallet, (Wallets?)null);

        DateTime capturedDay = default;
        _transferRepository
            .SumCompletedAmountForDayAsync(
                Arg.Any<UserId>(),
                Arg.Do<DateTime>(d => capturedDay = d),
                Arg.Any<CancellationToken>())
            .Returns(0m);

        await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        capturedDay.ShouldBe(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Handle_PassesSenderUserIdBuiltFromCurrentUserToTransferRepository()
    {
        var recipient = NewRecipient();
        var senderId = _currentUserService.UserId!.Value;

        _userRepository
            .GetByPhoneNumberAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(recipient);
        _walletRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(null, (Wallets?)null);

        UserId? capturedUserId = null;
        _transferRepository
            .SumCompletedAmountForDayAsync(
                Arg.Do<UserId>(u => capturedUserId = u),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(0m);

        await _sut.Handle(
            new PreviewWalletTransferQuery("09121234567", 20_000m),
            CancellationToken.None);

        capturedUserId.ShouldNotBeNull();
        capturedUserId!.Value.ShouldBe(senderId);
    }
}
