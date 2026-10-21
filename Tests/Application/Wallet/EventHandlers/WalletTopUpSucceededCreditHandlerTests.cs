using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WalletTopUpSucceededCreditHandlerTests : HandlerTestBase
{
    private readonly WalletTopUpSucceededCreditHandler _sut;

    public WalletTopUpSucceededCreditHandlerTests()
    {
        _sut = new WalletTopUpSucceededCreditHandler(AuditService);
    }

    private static WalletTopUpSucceededEvent BuildEvent(
        Guid? topUpGuid = null,
        Guid? userGuid = null,
        decimal amount = 100_000m,
        string gatewayRefId = "GW-REF-123")
    {
        return new WalletTopUpSucceededEvent(
            WalletTopUpId.From(topUpGuid ?? Guid.NewGuid()),
            UserId.From(userGuid ?? Guid.NewGuid()),
            Money.Create(amount),
            gatewayRefId);
    }

    private static DomainEventNotification<WalletTopUpSucceededEvent> Wrap(WalletTopUpSucceededEvent evt) => new(evt);

    [Fact]
    public async Task Handle_RecordsTheTopUpWithoutCreditingTheWalletAgain()
    {
        var evt = BuildEvent(amount: 500_000m, gatewayRefId: "GW-OK-1");

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await AuditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Wallet top-up succeeded")
                             && s!.Contains("500000")
                             && s!.Contains("GW-OK-1")),
            Arg.Any<CancellationToken>());
        await AuditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAudit()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), cts.Token);

        await AuditService.Received(1).LogInformationAsync(Arg.Any<string>(), cts.Token);
    }
}
