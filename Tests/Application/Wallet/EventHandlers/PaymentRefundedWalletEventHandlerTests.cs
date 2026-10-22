using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.DebitWallet;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using OrderId = Domain.Order.ValueObjects.OrderId;

namespace Tests.Application.Wallet.EventHandlers;

public class PaymentRefundedWalletEventHandlerTests : HandlerTestBase
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly PaymentRefundedWalletEventHandler _sut;

    public PaymentRefundedWalletEventHandlerTests()
    {
        _sut = new PaymentRefundedWalletEventHandler(_mediator, AuditService);
    }

    private static PaymentRefundedEvent BuildEvent(
        PaymentGateway gateway,
        decimal amount = 75_000m)
        => new(
            PaymentTransactionId.NewId(),
            OrderId.NewId(),
            UserId.NewId(),
            Money.Create(amount, "IRT"),
            null,
            gateway);

    [Fact]
    public async Task Handle_WhenThePaymentCameFromAGateway_ReversesTheWalletCredit()
    {
        _mediator
            .Send(Arg.Any<DebitWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        var evt = BuildEvent(PaymentGateway.Zarinpal);

        await _sut.Handle(
            new DomainEventNotification<PaymentRefundedEvent>(evt),
            CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<DebitWalletCommand>(c =>
                c!.UserId == evt.UserId.Value
                && c.Amount == 75_000m
                && c.TransactionType == WalletTransactionType.Refund
                && c.ReferenceType == WalletReferenceType.Payment
                && c.IdempotencyKey == $"refund-payment-{evt.PaymentTransactionId.Value}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenThePaymentCameFromTheWallet_DoesNotTouchTheWalletAgain()
    {
        var evt = BuildEvent(PaymentGateway.Wallet);

        await _sut.Handle(
            new DomainEventNotification<PaymentRefundedEvent>(evt),
            CancellationToken.None);

        await _mediator.DidNotReceiveWithAnyArgs().Send(default!, default);
    }

    [Fact]
    public async Task Handle_WhenTheWalletHasNoBalance_LogsTheDiscrepancyForReconciliation()
    {
        _mediator
            .Send(Arg.Any<DebitWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(Error.BusinessRule("WALLET_INSUFFICIENT", "not enough")));

        var evt = BuildEvent(PaymentGateway.Zarinpal);

        await _sut.Handle(
            new DomainEventNotification<PaymentRefundedEvent>(evt),
            CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "WalletRefundFailed",
            Arg.Is<string>(s => s != null && s!.Contains(evt.PaymentTransactionId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }
}
