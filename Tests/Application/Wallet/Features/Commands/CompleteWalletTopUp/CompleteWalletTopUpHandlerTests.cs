using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Application.Wallet.Features.Commands.CompleteWalletTopUp;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Wallet.Features.Commands.CompleteWalletTopUp;

public sealed class CompleteWalletTopUpHandlerTests
{
    private readonly IWalletTopUpRepository _topUpRepository = Substitute.For<IWalletTopUpRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IPaymentGatewayFactory _gatewayFactory = Substitute.For<IPaymentGatewayFactory>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();

    private readonly CompleteWalletTopUpHandler _sut;

    public CompleteWalletTopUpHandlerTests()
    {
        _gatewayFactory.GetGateway(Arg.Any<string>()).Returns(_gateway);
        _sut = new CompleteWalletTopUpHandler(_topUpRepository, _walletRepository, _gatewayFactory, _auditService);
    }

    [Fact]
    public async Task Handle_WhenAuthorityIsEmpty_ReturnsInvalidResult()
    {
        var result = await _sut.Handle(new CompleteWalletTopUpCommand("", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.StatusText.ShouldBe("invalid");
    }

    [Fact]
    public async Task Handle_WhenTopUpNotFound_ReturnsNotFoundResult()
    {
        _topUpRepository.GetByAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((WalletTopUp?)null);

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.StatusText.ShouldBe("not_found");
    }

    [Fact]
    public async Task Handle_WhenTopUpAlreadySucceeded_ReturnsIdempotentSuccess()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(100_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");
        topUp.MarkSucceeded("REF-9999");
        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeTrue();
        result.Value.StatusText.ShouldBe("succeeded");
    }

    [Fact]
    public async Task Handle_WhenStatusNotOk_MarksCancelledAndReturnsCancelled()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(100_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");
        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "NOK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.StatusText.ShouldBe("cancelled");
        topUp.Status.ShouldBe(WalletTopUpStatus.Cancelled);
        _topUpRepository.Received(1).Update(topUp);
    }

    [Fact]
    public async Task Handle_WhenVerifyFails_MarksFailedAndReturnsFailed()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(100_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");
        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(null, false, null, null, 0m));

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.StatusText.ShouldBe("failed");
        topUp.Status.ShouldBe(WalletTopUpStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenGatewayThrowsExternalServiceException_MarksFailedAndReturnsFailed()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(100_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");
        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns<Task<PaymentVerificationResult>>(_ => throw new ExternalServiceException("Zarinpal", "gateway down"));

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeFalse();
        result.Value.StatusText.ShouldBe("failed");
        topUp.Status.ShouldBe(WalletTopUpStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenVerifiedAndWalletExists_CreditsWalletAndReturnsSuccess()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(200_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");
        var wallet = new WalletBuilder().WithOwnerId(topUp.UserId).Build();

        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(Guid.NewGuid(), true, 123456L, "**********1234", 0m));
        _walletRepository.GetByUserIdForUpdateAsync(topUp.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeTrue();
        result.Value.StatusText.ShouldBe("succeeded");
        wallet.Balance.Amount.ShouldBe(200_000m);
        topUp.Status.ShouldBe(WalletTopUpStatus.Succeeded);
        await _auditService.Received(1).LogSystemEventAsync(
            "WalletTopUpSucceeded", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVerifiedAndWalletNotExists_CreatesWalletAndCreditsIt()
    {
        var topUp = new WalletTopUpBuilder().WithAmount(200_000m).Build();
        topUp.MarkAuthorityIssued("AUTH-1");

        Wallets? capturedWallet = null;
        _topUpRepository.GetByAuthorityAsync("AUTH-1", Arg.Any<CancellationToken>()).Returns(topUp);
        _gateway.VerifyAsync(Arg.Any<string>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(Guid.NewGuid(), true, 123456L, null, 0m));
        _walletRepository.GetByUserIdForUpdateAsync(topUp.UserId, Arg.Any<CancellationToken>())
            .Returns((Wallets?)null);
        await _walletRepository.AddAsync(Arg.Do<Wallets>(w => capturedWallet = w), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new CompleteWalletTopUpCommand("AUTH-1", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsSuccess.ShouldBeTrue();
        capturedWallet.ShouldNotBeNull();
        capturedWallet!.Balance.Amount.ShouldBe(200_000m);
    }
}
