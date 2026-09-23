using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Tests.Application.Wallet.EventHandlers;

public class PaymentRefundedWalletEventHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly PaymentRefundedWalletEventHandler _sut;

    public PaymentRefundedWalletEventHandlerTests()
    {
        _sut = new PaymentRefundedWalletEventHandler(_mediator, _auditService);
    }

    private static DomainEventNotification<PaymentRefundedEvent> Notification(
        PaymentTransactionId transactionId, OrderId orderId, UserId userId, decimal amount) =>
        new(new PaymentRefundedEvent(transactionId, orderId, userId, Money.Create(amount, "IRT"), null));

    [Fact]
    public async Task Handle_DispatchesRefundCreditWithPaymentReference()
    {
        var transactionId = PaymentTransactionId.NewId();
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Notification(transactionId, orderId, userId, 75_000m), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreditWalletCommand>(c =>
                c != null &&
                c.UserId == userId.Value &&
                c.Amount == 75_000m &&
                c.TransactionType == WalletTransactionType.Refund &&
                c.ReferenceType == WalletReferenceType.Payment &&
                c.ReferenceId == transactionId.Value.ToString() &&
                c.IdempotencyKey == $"refund-payment-{transactionId.Value}"),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenCreditFails_LogsWalletRefundFailed()
    {
        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(Error.BusinessRule("WALLET_LOCKED", "locked")));

        await _sut.Handle(
            Notification(PaymentTransactionId.NewId(), OrderId.NewId(), UserId.NewId(), 10m),
            CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletRefundFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMediatorThrows_LogsHandlerErrorAndDoesNotPropagate()
    {
        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns<ServiceResult<Unit>>(_ => throw new InvalidOperationException("bus down"));
        var transactionId = PaymentTransactionId.NewId();

        await Should.NotThrowAsync(() => _sut.Handle(
            Notification(transactionId, OrderId.NewId(), UserId.NewId(), 10m),
            CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletPaymentRefundedHandlerError",
            Arg.Is<string>(s => s != null && s.Contains(transactionId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }
}
