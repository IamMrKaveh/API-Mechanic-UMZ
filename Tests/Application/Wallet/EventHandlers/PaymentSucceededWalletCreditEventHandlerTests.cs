using Application.Common.Events;
using Application.Wallet.EventHandlers;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public class PaymentSucceededWalletCreditEventHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly PaymentSucceededWalletCreditEventHandler _sut;

    public PaymentSucceededWalletCreditEventHandlerTests()
    {
        _sut = new PaymentSucceededWalletCreditEventHandler(_mediator, _auditService);
    }

    private static PaymentSucceededEvent BuildEvent() => new(
        PaymentTransactionId.NewId(),
        OrderId.NewId(),
        refId: 987654321,
        UserId.NewId(),
        Money.Create(150_000m, "IRT"));

    [Fact]
    public async Task Handle_DispatchesCreditWalletCommandOnce()
    {
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        var notification = new DomainEventNotification<PaymentSucceededEvent>(BuildEvent());

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsPaymentTransactionAsIdempotencyKeyAndReference()
    {
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        var evt = BuildEvent();
        var notification = new DomainEventNotification<PaymentSucceededEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<CreditWalletCommand>(c =>
                c!.ReferenceId == evt.PaymentTransactionId.Value.ToString() &&
                c.IdempotencyKey == $"payment-topup-{evt.PaymentTransactionId}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMediatorReturnsSuccess_DoesNotAudit()
    {
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        var notification = new DomainEventNotification<PaymentSucceededEvent>(BuildEvent());

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenMediatorReturnsFailure_LogsWalletTopUpFailedAudit()
    {
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Failure(Error.BusinessRule("WALLET_LOCKED", "wallet is locked")));

        var notification = new DomainEventNotification<PaymentSucceededEvent>(BuildEvent());

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletTopUpFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMediatorThrows_LogsHandlerErrorAuditAndDoesNotPropagate()
    {
        _mediator
            .Send(Arg.Any<CreditWalletCommand>(), Arg.Any<CancellationToken>())
            .Returns<ServiceResult<Unit>>(_ => throw new InvalidOperationException("bus down"));

        var notification = new DomainEventNotification<PaymentSucceededEvent>(BuildEvent());

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletPaymentSucceededHandlerError",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
