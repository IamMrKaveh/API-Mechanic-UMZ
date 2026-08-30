using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WalletTopUpSucceededCreditHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly WalletTopUpSucceededCreditHandler _sut;

    public WalletTopUpSucceededCreditHandlerTests()
    {
        _sut = new WalletTopUpSucceededCreditHandler(_mediator, _auditService);
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
    public async Task Handle_WhenMediatorSucceeds_LogsInformation()
    {
        var evt = BuildEvent(amount: 500_000m, gatewayRefId: "GW-OK-1");
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Wallet credited from top-up")
                             && s.Contains("500000")
                             && s.Contains("GW-OK-1")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenMediatorFails_LogsError()
    {
        var evt = BuildEvent(gatewayRefId: "GW-FAIL-1");
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(
                new Error("CREDIT_FAIL", "credit failed", ErrorType.Failure)));

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("WalletTopUp credit application failed")
                             && s.Contains("GW-FAIL-1")),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogInformationAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenInvoked_SendsCreditWalletCommandWithExpectedPayload()
    {
        var topUpGuid = Guid.NewGuid();
        var userGuid = Guid.NewGuid();
        var evt = BuildEvent(topUpGuid: topUpGuid, userGuid: userGuid, amount: 250_000m, gatewayRefId: "GW-XYZ");
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreditWalletCommand>(c =>
                c!.UserId == userGuid
                && c.Amount == 250_000m
                && c.TransactionType == WalletTransactionType.Credit
                && c.ReferenceType == WalletReferenceType.TopUp
                && c.ReferenceId == topUpGuid.ToString()
                && c.IdempotencyKey == $"topup-credit:{topUpGuid}"
                && c.CorrelationId == "GW-XYZ"
                && c.Description != null
                && c.Description.Contains("GW-XYZ")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToMediatorAndAudit()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildEvent();
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), cts.Token)
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Wrap(evt), cts.Token);

        await _mediator.Received(1).Send(Arg.Any<CreditWalletCommand>(), cts.Token);
        await _auditService.Received(1).LogInformationAsync(Arg.Any<string>(), cts.Token);
    }
}
