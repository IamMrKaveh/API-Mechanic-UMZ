namespace Application.Order.Features.Commands.ExpireOrders;

public sealed class ExpireOrdersHandler : IRequestHandler<ExpireOrdersCommand, ExpireOrdersResult>
{
    private static readonly TimeSpan OrderExpiryWindow = TimeSpan.FromMinutes(30);

    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireOrdersHandler> _logger;

    public ExpireOrdersHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExpireOrdersHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExpireOrdersResult> Handle(ExpireOrdersCommand request, CancellationToken ct)
    {
        var expiryThreshold = DateTime.UtcNow.Subtract(OrderExpiryWindow);

        var expirableStatuses = new[]
        {
            OrderStatusValue.Pending.Value,
            OrderStatusValue.Created.Value,
        };

        var ordersToExpire = await _orderRepository
            .GetExpirableOrdersAsync(expiryThreshold, expirableStatuses, ct);

        if (!ordersToExpire.Any())
            return new ExpireOrdersResult(0, Array.Empty<int>());

        var expiredIds = new List<int>();

        foreach (var order in ordersToExpire)
        {
            try
            {
                order.Expire();
                await _orderRepository.UpdateAsync(order, ct);
                expiredIds.Add(order.Id);

                _logger.LogInformation(
                    "Order {OrderId} ({OrderNumber}) expired after {Window} minutes.",
                    order.Id, order.OrderNumber, OrderExpiryWindow.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire Order {OrderId}", order.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Expired {Count} orders.", expiredIds.Count);
        return new ExpireOrdersResult(expiredIds.Count, expiredIds);
    }
}