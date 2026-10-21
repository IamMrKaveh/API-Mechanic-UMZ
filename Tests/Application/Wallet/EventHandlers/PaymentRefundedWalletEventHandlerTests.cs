using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public class PaymentRefundedWalletEventHandlerTests : HandlerTestBase
{
    private readonly PaymentRefundedWalletEventHandler _sut;

    public PaymentRefundedWalletEventHandlerTests()
    {
        _sut = new PaymentRefundedWalletEventHandler(AuditService);
    }

    private static DomainEventNotification<PaymentRefundedEvent> Notification(
        PaymentTransactionId transactionId, OrderId orderId, UserId userId, decimal amount) =>
        new(new PaymentRefundedEvent(
            transactionId, orderId, userId, Money.Create(amount, "IRT"), null, PaymentGateway.Zarinpal));

    [Fact]
    public async Task Handle_RecordsTheRefundForReconciliation()
    {
        var transactionId = PaymentTransactionId.NewId();

        await _sut.Handle(
            Notification(transactionId, OrderId.NewId(), UserId.NewId(), 75_000m),
            CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "WalletPaymentRefunded",
            Arg.Is<string>(s => s != null && s!.Contains(transactionId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }
}
