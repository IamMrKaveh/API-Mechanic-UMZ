namespace Application.Wallet.EventHandlers;

/// <summary>
/// When an order is cancelled, release any wallet reservation.
/// </summary>
public class OrderCancelledWalletReleaseEventHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCancelledWalletReleaseEventHandler> _logger;

    public OrderCancelledWalletReleaseEventHandler(IMediator mediator, ILogger<OrderCancelledWalletReleaseEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[WalletRelease] OrderCancelled: OrderId={OrderId}, UserId={UserId}",
                notification.OrderId, notification.UserId);

            var command = new ReleaseWalletReservationCommand(
                notification.UserId,
                notification.OrderId);

            await _mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing wallet reservation for cancelled order {OrderId}", notification.OrderId);
        }
    }
}