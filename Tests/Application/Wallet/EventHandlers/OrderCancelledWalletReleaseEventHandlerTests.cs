using Application.Common.Events;
using Application.Wallet.Contracts;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Application.Wallet.Features.Shared;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Tests.Application.Wallet.EventHandlers;

public class OrderCancelledWalletReleaseEventHandlerTests : HandlerTestBase
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IWalletQueryService _queryService = Substitute.For<IWalletQueryService>();
    private readonly OrderCancelledWalletReleaseEventHandler _sut;

    public OrderCancelledWalletReleaseEventHandlerTests()
    {
        _sut = new OrderCancelledWalletReleaseEventHandler(_mediator, _queryService, AuditService);
    }

    private static DomainEventNotification<OrderCancelledEvent> Notification(OrderId orderId, UserId userId) =>
        new(new OrderCancelledEvent(orderId, OrderNumber.Create("ORD-1"), userId, "changed mind", false));

    [Fact]
    public async Task Handle_WhenPaymentEntryExists_DispatchesRefundCredit()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        _queryService.GetOrderPaymentLedgerEntryAsync(userId, orderId, Arg.Any<CancellationToken>())
            .Returns(new WalletLedgerEntryDto(
                Guid.NewGuid(), Guid.NewGuid(), userId.Value, 250_000m, 250_000m,
                "Debit", "Order", orderId.Value, null, DateTime.UtcNow, false));
        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Notification(orderId, userId), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreditWalletCommand>(c =>
                c != null &&
                c.UserId == userId.Value &&
                c.Amount == 250_000m &&
                c.TransactionType == WalletTransactionType.Refund &&
                c.ReferenceType == WalletReferenceType.Order &&
                c.ReferenceId == orderId.Value.ToString() &&
                c.IdempotencyKey == $"refund-order-{orderId.Value}"),
            Arg.Any<CancellationToken>());
        await AuditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenNoPaymentEntry_ReleasesReservation()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        _queryService.GetOrderPaymentLedgerEntryAsync(userId, orderId, Arg.Any<CancellationToken>())
            .Returns((WalletLedgerEntryDto?)null);
        _mediator.Send(Arg.Any<ReleaseWalletReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await _sut.Handle(Notification(orderId, userId), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<ReleaseWalletReservationCommand>(c =>
                c != null && c.UserId == userId.Value && c.WalletReservationId == orderId.Value),
            Arg.Any<CancellationToken>());
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRefundFails_LogsWalletRefundFailed()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        _queryService.GetOrderPaymentLedgerEntryAsync(userId, orderId, Arg.Any<CancellationToken>())
            .Returns(new WalletLedgerEntryDto(
                Guid.NewGuid(), Guid.NewGuid(), userId.Value, 10m, 10m,
                "Debit", "Order", orderId.Value, null, DateTime.UtcNow, false));
        _mediator.Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(Error.BusinessRule("WALLET_LOCKED", "locked")));

        await _sut.Handle(Notification(orderId, userId), CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "WalletRefundFailed",
            Arg.Is<string>(s => s != null && s.Contains(orderId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryThrows_LogsHandlerErrorAndDoesNotPropagate()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        _queryService.GetOrderPaymentLedgerEntryAsync(userId, orderId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        await Should.NotThrowAsync(() => _sut.Handle(Notification(orderId, userId), CancellationToken.None));

        await AuditService.Received(1).LogSystemEventAsync(
            "WalletOrderCancelledHandlerError",
            Arg.Is<string>(s => s != null && s.Contains("db down")),
            Arg.Any<CancellationToken>());
    }
}
