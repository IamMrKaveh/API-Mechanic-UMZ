namespace Application.Features.Orders.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    public UpdateOrderStatusHandler(
        IOrderRepository orderRepository,
        IOrderStatusRepository orderStatusRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IAuditService auditService)
    {
        _orderRepository = orderRepository;
        _orderStatusRepository = orderStatusRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    public async Task<ServiceResult> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetOrderByIdAsync(request.OrderId, null, true);
        if (order == null) return ServiceResult.Fail("Order not found", 404);

        if (!string.IsNullOrEmpty(request.Dto.RowVersion))
        {
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.Dto.RowVersion));
        }

        var newStatus = await _orderStatusRepository.GetByIdAsync(request.Dto.OrderStatusId);
        if (newStatus == null) return ServiceResult.Fail("Status not found", 404);

        var oldStatusName = order.OrderStatus?.Name ?? "Unknown";
        order.OrderStatusId = request.Dto.OrderStatusId;
        order.UpdatedAt = DateTime.UtcNow;

        _orderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send Notification
            await _notificationService.SendOrderStatusNotificationAsync(order.UserId, order.Id, oldStatusName, newStatus.Name);

            // Audit Log
            await _auditService.LogOrderEventAsync(order.Id, "UpdateOrderStatus", order.UserId, $"Changed from {oldStatusName} to {newStatus.Name}");

            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("Concurrency conflict", 409);
        }
    }
}