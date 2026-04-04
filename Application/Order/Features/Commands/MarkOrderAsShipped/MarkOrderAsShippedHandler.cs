using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Notification.Contracts;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Order.Interfaces;

namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IAuditService auditService,
    ILogger<MarkOrderAsShippedHandler> logger) : IRequestHandler<MarkOrderAsShippedCommand, ServiceResult>
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<MarkOrderAsShippedHandler> _logger = logger;

    public async Task<ServiceResult> Handle(
        MarkOrderAsShippedCommand request,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, ct);
        if (order == null)
            return ServiceResult.NotFound("سفارش یافت نشد.");

        if (!string.IsNullOrEmpty(request.RowVersion))
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(request.RowVersion));

        var oldStatusName = order.Status.DisplayName;

        try
        {
            order.Ship();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Unexpected(ex.Message);
        }

        await _orderRepository.UpdateAsync(order, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);

            await _notificationService.SendOrderStatusNotificationAsync(
                order.UserId,
                order.Id,
                oldStatusName,
                OrderStatusValue.Shipped.DisplayName);

            await _auditService.LogOrderEventAsync(
                order.Id,
                "ShipOrder",
                request.UpdatedByUserId,
                "سفارش ارسال شد.");

            _logger.LogInformation("Order {OrderId} shipped", order.Id);

            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Conflict("این سفارش توسط کاربر دیگری تغییر کرده است. لطفاً صفحه را رفرش کنید.");
        }
    }
}